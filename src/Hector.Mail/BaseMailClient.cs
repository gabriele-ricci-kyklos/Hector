namespace Hector.Core.Mail
{
    public record SMTPOptions(string Host, int Port, string Username, string Password, string Sender, bool EnableSSL)
    {
        public SMTPOptions() : this(string.Empty, 0, string.Empty, string.Empty, string.Empty, false) { }
    }

    public abstract class BaseMailClient(SMTPOptions options)
    {
        protected readonly SMTPOptions _options = ValidateOptions(options);

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
    }
}
