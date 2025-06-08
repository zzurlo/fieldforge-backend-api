using System;

namespace FieldForge.Api.Models;

public class UserRole
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public int RoleId { get; set; }
}