using System.Net.Mail;
using System.Text;

namespace Hector.Core.Mail
{
    public class MailModel : IDisposable
    {
        private readonly List<Attachment> _attachments;

        public bool ContainsHtml { get; }

        public string Subject { get; }

        public StringBuilder BodyBuilder = new();
        public StringBuilder FooterBuilder = new();

        public string? Sender { get; }
        public string[] ToAddressList { get; }
        public string[]? CCAddressList { get; }
        public string[]? BCCAddressList { get; }

        public string Body => BodyBuilder.ToString();
        public string Footer => FooterBuilder.ToString();

        public IReadOnlyList<Attachment> Attachments => _attachments;

        public MailModel(bool containsHtml, string subject, string body, string? footer, string[] toAddressList, string[]? ccAddressList = null, string[]? bccAddressList = null)
            : this(containsHtml, null, subject, body, footer, toAddressList, ccAddressList, bccAddressList)
        {
        }

        public MailModel(bool containsHtml, string? sender, string subject, string body, string? footer, string[] toAddressList, string[]? ccAddressList = null, string[]? bccAddressList = null)
        {
            _attachments = [];

            BodyBuilder = new(body);
            FooterBuilder = new(footer);

            ContainsHtml = containsHtml;
            Sender = sender;
            Subject = subject;

            ToAddressList = toAddressList;
            CCAddressList = ccAddressList;
            BCCAddressList = bccAddressList;
        }

        public void AddAttachment(string name, byte[] imageBytes)
        {
            Attachment attachment = new(new MemoryStream(imageBytes), name);
            _attachments.Add(attachment);
        }

        public void AddAttachment(string name, Stream stream)
        {
            Attachment attachment = new(stream, name);
            _attachments.Add(attachment);
        }

        public MailMessage ToMailMessage(string sender)
        {
            MailMessage mailMessage = new()
            {
                From = new MailAddress(Sender ?? sender),
                Subject = Subject,
                IsBodyHtml = ContainsHtml
            };

            string? mailBody = $"{Body}{Environment.NewLine}{Footer}";

            if (_attachments.Count != 0)
            {
                foreach (Attachment res in _attachments)
                {
                    mailMessage.Attachments.Add(res);
                }
            }

            mailMessage.IsBodyHtml = ContainsHtml;
            mailMessage.Body = mailBody;
            mailMessage.To.Add(ToAddressList.StringJoin(","));

            if (!CCAddressList.IsNullOrEmptyList())
            {
                mailMessage.CC.Add(CCAddressList!.StringJoin(","));
            }

            if (!BCCAddressList.IsNullOrEmptyList())
            {
                mailMessage.Bcc.Add(BCCAddressList!.StringJoin(","));
            }

            return mailMessage;
        }

        public void Dispose()
        {
            _attachments
                .ForEach(x => x?.Dispose());
        }
    }
}
