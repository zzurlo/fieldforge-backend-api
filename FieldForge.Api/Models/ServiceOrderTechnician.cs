using System;

namespace FieldForge.Api.Models;

public class ServiceOrderTechnician
{
    public Guid Id { get; set; }
    public Guid ServiceOrderId { get; set; }
    public string TechnicianUserId { get; set; } = string.Empty;

    // Navigation property
    public ServiceOrder? ServiceOrder { get; set; }
}