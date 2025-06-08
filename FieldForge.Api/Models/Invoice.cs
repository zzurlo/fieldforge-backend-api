using System;
using System.Collections.Generic;

namespace FieldForge.Api.Models;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ServiceOrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal AmountDue { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<InvoiceLineItem> LineItems { get; set; } = new();

    public ServiceOrder? ServiceOrder { get; set; }
    public Customer? Customer { get; set; }
}