using System;

namespace FieldForge.Api.Models;

public class ServiceOrder
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public Company Company { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public ICollection<ServiceOrderTechnician> ServiceOrderTechnicians { get; set; } = new List<ServiceOrderTechnician>();
}