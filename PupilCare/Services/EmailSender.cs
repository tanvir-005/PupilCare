using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Threading.Tasks;

namespace PupilCare.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                smtpSettings["SenderName"] ?? "PupilCare", 
                smtpSettings["SenderEmail"] ?? "no-reply@pupilcare.com"
            ));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // For demo/development, we might want to bypass certificate validation if using a local mock server
            // client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync(
                smtpSettings["Server"], 
                int.Parse(smtpSettings["Port"] ?? "587"), 
                SecureSocketOptions.StartTls
            );

            await client.AuthenticateAsync(smtpSettings["Username"], smtpSettings["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
