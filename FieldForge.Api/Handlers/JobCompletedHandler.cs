using MediatR;
using Microsoft.EntityFrameworkCore;
using FieldForge.Api.Events;
using FieldForge.Api.Data;
using FieldForge.Api.Models;

namespace FieldForge.Api.Handlers
{
    public class JobCompletedHandler : INotificationHandler<JobCompletedEvent>
    {
        private readonly ApplicationDbContext _context;

        public JobCompletedHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(JobCompletedEvent notification, CancellationToken cancellationToken)
        {
            var serviceOrder = await _context.ServiceOrders
                .Include(so => so.Customer)
                .FirstOrDefaultAsync(so => so.Id == notification.ServiceOrderId, cancellationToken);

            if (serviceOrder == null)
            {
                return;
            }

            // Create invoice with flat rate of $100.00
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                CustomerId = serviceOrder.CustomerId,
                CompanyId = serviceOrder.CompanyId,
                ServiceOrderId = serviceOrder.Id,
                CreatedOn = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                Status = "Pending",
                AmountDue = 100.00m
            };

            var lineItem = new InvoiceLineItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                Description = $"Service Call - {serviceOrder.Description}",
                Quantity = 1,
                UnitPrice = 100.00m
            };

            _context.Invoices.Add(invoice);
            _context.InvoiceLineItems.Add(lineItem);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}