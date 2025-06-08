namespace FieldForge.Api.Models.Dto;

public class InviteEmployeeDto
{
    public Guid CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}