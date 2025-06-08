namespace FieldForge.Api.Models.Dto;

public class AssignUserRoleDto
{
    public Guid CompanyId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}