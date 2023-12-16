using MailKit.Security;
using MimeKit;
using System.Net.Mail;

namespace Hector.Core.Mail
{
    public class MailKitMailClient(SMTPOptions options) : BaseMailClient(options), IMailClient
    {
        private async Task<MailKit.Net.Smtp.SmtpClient> NewSmtpClient()
        {
            MailKit.Net.Smtp.SmtpClient smtpClient = new();

            await smtpClient
                .ConnectAsync(_options.Host, _options.Port, _options.EnableSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None)
                .ConfigureAwait(false);

            await smtpClient
                .AuthenticateAsync(_options.Username, _options.Password)
                .ConfigureAwait(false);

            return smtpClient;
        }

        public async Task SendMailAsync(MailModel mailModel)
        {
            using MailKit.Net.Smtp.SmtpClient smtpClient = await NewSmtpClient().ConfigureAwait(false);
            using MailMessage mailMessage = mailModel.ToMailMessage(_options.Sender);
            using MimeMessage msg = MimeMessage.CreateFromMailMessage(mailMessage);
            await smtpClient.SendAsync(msg).ConfigureAwait(false);
            await smtpClient.DisconnectAsync(true).ConfigureAwait(false);
        }

        public async Task SendMailAsync(bool containsHtml, string subject, string body, string? footer, string? sender, string[] toAddressList)
        {
            using MailModel model = new(containsHtml, sender ?? _options.Sender, subject, body, footer, toAddressList);
            await SendMailAsync(model).ConfigureAwait(false);
        }

        public Task SendMailAsync(string subject, string body, string sender, string[] toAddressList) =>
            SendMailAsync(false, subject, body, null, sender, toAddressList);

        public Task SendMailAsync(string subject, string body, string[] toAddressList) =>
            SendMailAsync(false, subject, body, null, null, toAddressList);
    }
}
