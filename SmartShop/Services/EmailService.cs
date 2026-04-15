using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SmartShop.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            var senderEmail = _config["Email:SenderEmail"] ?? string.Empty;

            message.From.Add(new MailboxAddress(
                _config["Email:SenderName"] ?? "SmartShop",
                senderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(
                senderEmail,
                _config["Email:AppPassword"] ?? string.Empty);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
