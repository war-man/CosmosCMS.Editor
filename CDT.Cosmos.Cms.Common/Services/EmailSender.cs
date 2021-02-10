using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CDT.Cosmos.Cms.Common.Services
{
    public class EmailSender : IEmailSender
    {
        public EmailSender(IOptions<AuthMessageSenderOptions> options)
        {
            Options = options.Value;
        }

        public AuthMessageSenderOptions Options { get; } //set only via Secret Manager

        public Response Response { get; private set; }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Execute(subject, message, email);
        }

        public Task Execute(string subject, string message, string email, string emailFrom = null)
        {
            var client = new SendGridClient(Options.SendGridKey);
            var msg = new SendGridMessage
            {
                From = new EmailAddress(emailFrom ?? Options.EmailFrom),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(email));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(true, true);

            Response = client.SendEmailAsync(msg).Result;

            return Task.CompletedTask;
        }
    }
}