using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using FieldForge.Api.Data;
using FieldForge.Api.Models;
using FieldForge.Api.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace FieldForge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly GraphServiceClient _graphClient;

    public AuthController(ApplicationDbContext context, GraphServiceClient graphClient)
    {
        _context = context;
        _graphClient = graphClient;
    }


    [HttpGet("test")]
    [Authorize]
    public IActionResult Test()
    {
        return Ok("Authenticated");
    }

    [HttpPost("register-company")]
    public async Task<IActionResult> RegisterCompany([FromBody] RegisterCompanyDto request)
    {
        // Validate email domain matches company domain
        var emailDomain = request.AdminEmail.Split('@')[1];
        if (!emailDomain.Equals(request.Domain, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Admin email domain must match company domain");
        }

        // Create company record
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.CompanyName,
            Domain = request.Domain,
            CreatedOn = DateTime.UtcNow
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        // Send B2B invitation
        var invitation = new Invitation
        {
            InvitedUserEmailAddress = request.AdminEmail,
            InviteRedirectUrl = "https://yourapp.com/post-invitation",
            SendInvitationMessage = true
        };
        var inviteResponse = await _graphClient.Invitations
            .PostAsync(invitation);

        return Created(
            $"/api/companies/{company.Id}",
            new
            {
                CompanyId = company.Id,
                InvitationUrl = inviteResponse.InviteRedeemUrl
            });
    }



    [HttpPost("invite-employee")]
    [Authorize(Policy = "RequireOrganizationAdmin")]
    public async Task<IActionResult> InviteEmployee([FromBody] InviteEmployeeDto request)
    {
        // Get user's tenant ID from claims
        var tenantId = User.FindFirst("tid")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized("Unable to determine tenant ID");
        }

        // Verify company belongs to caller's tenant
        var company = await _context.Companies.FindAsync(request.CompanyId);
        if (company == null || company.TenantId != tenantId)
        {
            return Forbid("Company does not belong to caller's tenant");
        }

        // Send B2B invitation
        var invitation = new Invitation
        {
            InvitedUserEmailAddress = request.Email,
            InviteRedirectUrl = "https://yourapp.com/post-invitation",
            SendInvitationMessage = true
        };

        var inviteResponse = await _graphClient.Invitations
            .PostAsync(invitation);

        // Save employee invite record
        var employeeInvite = new EmployeeInvite
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            Email = request.Email,
            Role = request.Role,
            Status = "Pending",
            SentOn = DateTime.UtcNow
        };

        _context.EmployeeInvites.Add(employeeInvite);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            InviteId = employeeInvite.Id,
            InvitationUrl = inviteResponse.InviteRedeemUrl
        });
    }
    

    [HttpPut("assign-role")]
    [Authorize(Policy = "RequireOrganizationAdmin")]
    public async Task<IActionResult> AssignRole([FromBody] AssignUserRoleDto request)
    {
        // Get user's tenant ID from claims
        var tenantId = User.FindFirst("tid")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized("Unable to determine tenant ID");
        }

        // Verify company belongs to caller's tenant
        var company = await _context.Companies.FindAsync(request.CompanyId);
        if (company == null || company.TenantId != tenantId)
        {
            return Forbid("Company does not belong to caller's tenant");
        }

        // Look up role by name
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == request.RoleName);
        if (role == null)
        {
            return BadRequest($"Role '{request.RoleName}' not found");
        }

        // Find existing user role or create new one
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => 
                ur.CompanyId == request.CompanyId && 
                ur.UserId == request.UserId);

        if (userRole == null)
        {
            userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                CompanyId = request.CompanyId,
                UserId = request.UserId,
                RoleId = role.Id
            };
            _context.UserRoles.Add(userRole);
        }
        else
        {
            userRole.RoleId = role.Id;
            _context.UserRoles.Update(userRole);
        }

        await _context.SaveChangesAsync();

        return Ok(new { UserRoleId = userRole.Id });
    }

}