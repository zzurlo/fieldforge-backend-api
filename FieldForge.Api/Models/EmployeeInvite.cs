using System;

namespace FieldForge.Api.Models;

public class EmployeeInvite
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime SentOn { get; set; }
    public string Status { get; set; } = string.Empty;
}