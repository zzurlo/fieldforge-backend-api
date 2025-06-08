using MediatR;

namespace FieldForge.Api.Events
{
    public class JobCompletedEvent : INotification
    {
        public Guid ServiceOrderId { get; set; }
        public Guid CompanyId { get; set; }
    }
}