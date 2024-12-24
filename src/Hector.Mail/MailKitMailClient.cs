using MailKit.Security;
using MimeKit;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Hector.Mail
{
    public record SMTPOptions(string Host, int Port, string Username, string Password, string Sender, bool EnableSSL)
    {
        public SMTPOptions() : this(string.Empty, 0, string.Empty, string.Empty, string.Empty, false) { }
    }

    public class MailKitMailClient(SMTPOptions options) : IMailClient
    {
        private readonly SMTPOptions _options = ValidateOptions(options);

        private static SMTPOptions ValidateOptions(SMTPOptions options)
        {
            string sender = options.Sender.ToNullIfBlank() ?? options.Username;

            return
                new SMTPOptions
                (
                    options.Host.ToNullIfBlank() ?? throw new ArgumentNullException(nameof(options.Host), "Invalid host"),
                    options.Port,
                    options.Username.ToNullIfBlank() ?? throw new ArgumentNullException(nameof(options.Username), "Invalid username"),
                    options.Password.ToNullIfBlank() ?? throw new ArgumentNullException(nameof(options.Password), "Invalid password"),
                    sender,
                    options.EnableSSL
                );
        }

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

        public async Task SendMailAsync(bool containsHtml, string subject, string body, string? footer, string? sender, string[] toAddressList, string[]? ccAddressList = null, string[]? bccAddressList = null)
        {
            using MailModel model = new(containsHtml, sender ?? _options.Sender, subject, body, footer, toAddressList, ccAddressList, bccAddressList);
            await SendMailAsync(model).ConfigureAwait(false);
        }

        public Task SendMailAsync(string subject, string body, string sender, string[] toAddressList, string[]? ccAddressList = null, string[]? bccAddressList = null) =>
            SendMailAsync(false, subject, body, null, sender, toAddressList, ccAddressList, bccAddressList);

        public Task SendMailAsync(string subject, string body, string[] toAddressList, string[]? ccAddressList = null, string[]? bccAddressList = null) =>
            SendMailAsync(false, subject, body, null, null, toAddressList, ccAddressList, bccAddressList);
    }
}
