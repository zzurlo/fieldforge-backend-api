using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FieldForge.Api.Models;

namespace FieldForge.Api.Services
{
    public class InvoicePdfService
    {
        public byte[] GenerateInvoicePdf(Invoice invoice, List<InvoiceLineItem> items, Customer customer)
        {
            // Configure QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

            // Generate PDF document
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(ComposeHeader);
                    
                    page.Content().Element(compose =>
                    {
                        // compose.Spacing(20);

                        // Customer Information
                        compose.Component(new CustomerInfoComponent(customer));

                        // Invoice Details
                        compose.Component(new InvoiceInfoComponent(invoice));

                        // Items Table
                        compose.Component(new ItemsTableComponent(items));

                        // Total Section
                        compose.Component(new TotalComponent(items));
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            // Generate PDF bytes
            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("FieldForge")
                        .FontSize(20)
                        .Bold();
                    column.Item().Text("Professional Field Services");
                    column.Item().Text("Invoice");
                });

                row.ConstantItem(100).Image("logo.png");
            });
        }

        private class CustomerInfoComponent : IComponent
        {
            private readonly Customer _customer;

            public CustomerInfoComponent(Customer customer)
            {
                _customer = customer;
            }

            public void Compose(IContainer container)
            {
                container.Column(column =>
                {
                    column.Item().Text("Bill To:").Bold();
                    column.Item().Text(_customer.Name);
                    column.Item().Text(_customer.AddressLine);
                    column.Item().Text($"{_customer.City}, {_customer.State} {_customer.Zip}");
                });
            }
        }

        private class InvoiceInfoComponent : IComponent
        {
            private readonly Invoice _invoice;

            public InvoiceInfoComponent(Invoice invoice)
            {
                _invoice = invoice;
            }

            public void Compose(IContainer container)
            {
                container.Column(column =>
                {
                    column.Item().Text($"Invoice #: {_invoice.Id}");
                    column.Item().Text($"Date: {_invoice.CreatedOn:d}");
                    column.Item().Text($"Due Date: {_invoice.DueDate:d}");
                });
            }
        }

        private class ItemsTableComponent : IComponent
        {
            private readonly List<InvoiceLineItem> _items;

            public ItemsTableComponent(List<InvoiceLineItem> items)
            {
                _items = items;
            }

            public void Compose(IContainer container)
            {
                container.Table(table =>
                {
                    // Define columns
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    // Add header row
                    table.Header(header =>
                    {
                        header.Cell().Text("Description").Bold();
                        header.Cell().Text("Quantity").Bold();
                        header.Cell().Text("Unit Price").Bold();
                        header.Cell().Text("Total").Bold();
                    });

                    // Add data rows
                    foreach (var item in _items)
                    {
                        table.Cell().Text(item.Description);
                        table.Cell().Text(item.Quantity.ToString());
                        table.Cell().Text($"${item.UnitPrice:F2}");
                        table.Cell().Text($"${item.Quantity * item.UnitPrice:F2}");
                    }
                });
            }
        }

        private class TotalComponent : IComponent
        {
            private readonly List<InvoiceLineItem> _items;

            public TotalComponent(List<InvoiceLineItem> items)
            {
                _items = items;
            }

            public void Compose(IContainer container)
            {
                var subtotal = _items.Sum(x => x.Quantity * x.UnitPrice);
                var tax = subtotal * 0.1m; // 10% tax rate
                var total = subtotal + tax;

                container.AlignRight().Column(column =>
                {
                    column.Item().Text($"Subtotal: ${subtotal:F2}");
                    column.Item().Text($"Tax (10%): ${tax:F2}");
                    column.Item().Text($"Total: ${total:F2}").Bold();
                });
            }
        }
    }
}