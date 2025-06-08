namespace FieldForge.Api.Models.Dto;

public class RegisterCompanyDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
}