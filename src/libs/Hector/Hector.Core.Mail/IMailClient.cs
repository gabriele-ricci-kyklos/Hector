namespace Hector.Core.Mail
{
    public interface IMailClient
    {
        Task SendMailAsync(MailModel mailModel);
        Task SendMailAsync(string subject, string body, string[] toAddressList);
        Task SendMailAsync(string subject, string body, string sender, string[] toAddressList);
        Task SendMailAsync(bool containsHtml, string subject, string body, string? footer, string sender, string[] toAddressList);
    }
}
