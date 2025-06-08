using Azure;
using Azure.Communication.Email;
using Azure.Communication.Sms;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace FieldForge.Api.Services
{
    public interface INotificationService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendSmsAsync(string toPhone, string messageText);
    }

    public class NotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly EmailClient _emailClient;
        private readonly SmsClient _smsClient;
        private readonly string _senderEmail;

        public NotificationService(IConfiguration configuration)
        {
            _configuration = configuration;
            var connectionString = _configuration.GetConnectionString("AzureCommunicationServices");
            
            _emailClient = new EmailClient(connectionString);
            _smsClient = new SmsClient(connectionString);
            _senderEmail = _configuration["AzureCommunicationServices:SenderEmail"];
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var emailContent = new EmailContent(subject)
                {
                    PlainText = body,
                    Html = body
                };

                var emailMessage = new EmailMessage(
                    senderAddress: _senderEmail,
                    recipientAddress: toEmail,
                    content: emailContent
                );

                var response = await _emailClient.SendAsync(
                    WaitUntil.Started,
                    emailMessage
                );
            }
            catch (RequestFailedException ex)
            {
                // Handle or log the exception appropriately
                throw new ApplicationException("Failed to send email", ex);
            }
        }

        public async Task SendSmsAsync(string toPhone, string messageText)
        {
            try
            {
                var response = await _smsClient.SendAsync(
                    from: _configuration["AzureCommunicationServices:PhoneNumber"],
                    to: toPhone,
                    message: messageText
                );

                if (response.Value.Successful)
                {
                    // Message sent successfully
                    return;
                }

                throw new ApplicationException($"Failed to send SMS: {response.Value.ErrorMessage}");
            }
            catch (RequestFailedException ex)
            {
                // Handle or log the exception appropriately
                throw new ApplicationException("Failed to send SMS", ex);
            }
        }
    }
}