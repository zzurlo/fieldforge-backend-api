using Azure.Communication.Email;
using FieldForge.Api.Data;
using FieldForge.Api.Models;
using FieldForge.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldForge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly InvoicePdfService _pdfService;
        private readonly EmailClient _emailClient;

        public BillingController(
            ApplicationDbContext context,
            InvoicePdfService pdfService,
            EmailClient emailClient)
        {
            _context = context;
            _pdfService = pdfService;
            _emailClient = emailClient;
        }

        [HttpPost("email-invoice/{invoiceId}")]
        [Authorize(Policy = "RequireBiller")]
        public async Task<IActionResult> EmailInvoice(Guid invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.LineItems)
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                return NotFound();

            var pdfBytes = _pdfService.GenerateInvoicePdf(
                invoice: invoice,
                items: invoice.LineItems.ToList(),
                customer: invoice.Customer
            );

            var emailContent = new EmailContent($"Invoice #{invoice.Id} from FieldForge")
            {
                PlainText = $"Please find attached your invoice #{invoice.Id}."
            };

            var emailAttachment = new EmailAttachment(
                name: $"invoice-{invoice.Id}.pdf",
                content: BinaryData.FromBytes(pdfBytes),
                contentType: "application/pdf"
            );

             var emailMessage = new EmailMessage(
                senderAddress: "invoicing@your-domain.com",
                recipientAddress: invoice.Customer.Email,
                content: emailContent
            );

            emailMessage.Attachments.Add(emailAttachment);

            await _emailClient.SendAsync(
                wait: Azure.WaitUntil.Completed,
                message: emailMessage
            );

            invoice.Status = "Sent";

            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
