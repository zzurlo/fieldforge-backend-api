// filepath: /workspaces/fieldforge-backend-api/FieldForge.Api/Controllers/ServiceOrderController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FieldForge.Api.Data;
using FieldForge.Api.Models;
using FieldForge.Api.Models.Dto;
using Microsoft.AspNetCore.SignalR;
using FieldForge.Api.Hubs;
using FieldForge.Api.Services;
using FieldForge.Api.Events;
using MediatR;


namespace FieldForge.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ServiceOrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly INotificationService _notificationService;
        private readonly IMediator _mediator;

        public ServiceOrderController(
        ApplicationDbContext context,
        IHubContext<NotificationHub> hubContext,
        INotificationService notificationService,
        IMediator mediator)
        {
            _context = context;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _mediator = mediator;
        }

        [HttpPut("{orderId}/assign")]
        [Authorize(Policy = "RequireOrganizationAdmin")]
        public async Task<IActionResult> AssignTechnicians(Guid orderId, [FromBody] AssignServiceOrderDto request)
        {
            // Get user's tenant ID from claims
            var tenantId = User.FindFirst("tid")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized("Unable to determine tenant ID");
            }

            // Load service order and verify company tenant
            var serviceOrder = await _context.ServiceOrders
                .Include(so => so.Company)
                .FirstOrDefaultAsync(so => so.Id == orderId);

            if (serviceOrder == null)
            {
                return NotFound("Service order not found");
            }

            if (serviceOrder.Company.TenantId != tenantId)
            {
                return Forbid("Service order does not belong to caller's tenant");
            }

            var existingAssignments = await _context.ServiceOrderTechnicians
                .Where(sot => sot.ServiceOrderId == orderId)
                .ToListAsync();
            _context.ServiceOrderTechnicians.RemoveRange(existingAssignments);

            // Create new assignments
            foreach (var userId in request.TechnicianUserIds)
            {
                var assignment = new ServiceOrderTechnician
                {
                    Id = Guid.NewGuid(),
                    ServiceOrderId = orderId,
                    TechnicianUserId = userId
                };
                _context.ServiceOrderTechnicians.Add(assignment);
            }

            // Save changes
            await _context.SaveChangesAsync();

            // Notify technicians via SignalR
            foreach (var userId in request.TechnicianUserIds)
            {
                await _hubContext.Clients.User(userId)
                    .SendAsync("ReceiveAssignmentUpdate", orderId);
            }

            return Ok();
        }

        [HttpPost]
        [Authorize(Policy = "RequireOrganizationAdmin")]
        public async Task<IActionResult> CreateServiceOrder([FromBody] CreateServiceOrderDto request)
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

            // Verify customer exists and belongs to company
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.CompanyId == request.CompanyId);
            if (customer == null)
            {
                return NotFound("Customer not found or does not belong to company");
            }

            var serviceOrder = new ServiceOrder
            {
                Id = Guid.NewGuid(),
                CompanyId = request.CompanyId,
                CustomerId = request.CustomerId,
                AddressLine = request.AddressLine,
                City = request.City,
                State = request.State,
                Zip = request.Zip,
                Description = request.Description,
                ScheduledDate = request.ScheduledDate,
                Status = "Scheduled",
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                LastUpdated = DateTime.UtcNow
            };

            _context.ServiceOrders.Add(serviceOrder);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetServiceOrder),
                new { id = serviceOrder.Id },
                serviceOrder);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetServiceOrder(Guid id)
        {
            var serviceOrder = await _context.ServiceOrders.FindAsync(id);
            if (serviceOrder == null)
            {
                return NotFound();
            }

            return Ok(serviceOrder);
        }


        [HttpPut("{orderId}/status")]
        [Authorize(Policy = "RequireTechnician")]
        public async Task<IActionResult> UpdateServiceOrderStatus(Guid orderId, [FromBody] UpdateServiceOrderStatusDto request)
        {
            // Get user's tenant ID from claims
            var tenantId = User.FindFirst("tid")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized("Unable to determine tenant ID");
            }

            // Load service order and verify company tenant
            var serviceOrder = await _context.ServiceOrders
                .Include(so => so.Company)
                .FirstOrDefaultAsync(so => so.Id == orderId);

            if (serviceOrder == null)
            {
                return NotFound("Service order not found");
            }

            if (serviceOrder.Company.TenantId != tenantId)
            {
                return Forbid("Service order does not belong to caller's tenant");
            }

            // Update status
            serviceOrder.Status = request.NewStatus;
            serviceOrder.LastUpdated = DateTime.UtcNow;

            // Save changes
            await _context.SaveChangesAsync();

            // If status is completed, publish event
            if (request.NewStatus == "Completed")
            {
                var completedEvent = new JobCompletedEvent
                {
                    ServiceOrderId = serviceOrder.Id,
                    CompanyId = serviceOrder.CompanyId
                };
                
                await _mediator.Publish(completedEvent);
            }

            return Ok();
        }


        [HttpGet("assigned")]
        [Authorize(Policy = "RequireTechnician")]
        public async Task<IActionResult> GetAssignedServiceOrders([FromQuery] string technicianUserId)
        {
            // Verify caller is requesting their own assignments
            var callerOid = User.FindFirst("oid")?.Value;
            if (string.IsNullOrEmpty(callerOid) || callerOid != technicianUserId)
            {
                return Forbid("Can only request own assignments");
            }

            var assignments = await _context.ServiceOrderTechnicians
                .Where(sot => sot.TechnicianUserId == technicianUserId)
                .Include(sot => sot.ServiceOrder)
                .ThenInclude(so => so.Customer)
                .Select(sot => new
                {
                    OrderId = sot.ServiceOrderId,
                    CustomerName = sot.ServiceOrder.Customer.Name,
                    Address = $"{sot.ServiceOrder.AddressLine}, {sot.ServiceOrder.City}, {sot.ServiceOrder.State} {sot.ServiceOrder.Zip}",
                    sot.ServiceOrder.ScheduledDate,
                    sot.ServiceOrder.Status,
                    sot.ServiceOrder.Latitude,
                    sot.ServiceOrder.Longitude
                })
                .ToListAsync();

            return Ok(assignments);
        }

        [HttpGet("calendar")]
        [Authorize]
        public async Task<IActionResult> GetCalendarEvents(
            [FromQuery] Guid companyId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            // Get user's tenant ID from claims
            var tenantId = User.FindFirst("tid")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized("Unable to determine tenant ID");
            }

            // Verify company belongs to caller's tenant
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null || company.TenantId != tenantId)
            {
                return Forbid("Company does not belong to caller's tenant");
            }

            var events = await _context.ServiceOrders
                .Where(so => 
                    so.CompanyId == companyId &&
                    so.ScheduledDate >= startDate &&
                    so.ScheduledDate <= endDate)
                .Select(so => new
                {
                    so.Id,
                    so.ScheduledDate,
                    Title = so.Description,
                    so.Status
                })
                .ToListAsync();

            return Ok(events);
        }
    


        [HttpPut("{orderId}/reschedule")]
        [Authorize(Policy = "RequireOrganizationAdmin")]
        public async Task<IActionResult> RescheduleServiceOrder(Guid orderId, [FromBody] RescheduleServiceOrderDto request)
        {
            // Get user's tenant ID from claims
            var tenantId = User.FindFirst("tid")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized("Unable to determine tenant ID");
            }

            // Load service order and verify company tenant
            var serviceOrder = await _context.ServiceOrders
                .Include(so => so.Company)
                .FirstOrDefaultAsync(so => so.Id == orderId);

            if (serviceOrder == null)
            {
                return NotFound("Service order not found");
            }

            if (serviceOrder.Company.TenantId != tenantId)
            {
                return Forbid("Service order does not belong to caller's tenant");
            }

            // Update scheduling info
            serviceOrder.ScheduledDate = request.ScheduledDate;
            serviceOrder.LastUpdated = DateTime.UtcNow;

            // Save changes
            await _context.SaveChangesAsync();

            // Get company admins
            var adminRoleId = 1; // Assuming 1 is the admin role ID
            var companyAdmins = await _context.UserRoles
                .Where(ur => ur.CompanyId == serviceOrder.CompanyId && ur.RoleId == adminRoleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            // Notify each admin
            foreach (var adminId in companyAdmins)
    {
                // In a real implementation, you would fetch these from user profile
                var adminEmail = $"admin_{adminId}@company.com"; // Stubbed email
                var adminPhone = $"+1555{adminId:D6}"; // Stubbed phone

                await _notificationService.SendEmailAsync(
                    adminEmail,
                    "Service Order Rescheduled",
                    $"Service order {orderId} has been rescheduled to {request.ScheduledDate:g}");

                await _notificationService.SendSmsAsync(
                    adminPhone,
                    $"Service order {orderId} has been rescheduled to {request.ScheduledDate:g}");
            }

            return Ok();
        }
    }
}