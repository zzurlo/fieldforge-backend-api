namespace FieldForge.Api.Models.Dto;

public class CreateServiceOrderDto
{
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}