using System.Threading.Tasks;

namespace Hector.Mail
{
    public interface IMailClient
    {
        Task SendMailAsync(MailModel mailModel);
        Task SendMailAsync(string subject, string body, string[] toAddressList, string[]? ccAddressList = null, string[]? bccAddressList = null);
        Task SendMailAsync(string subject, string body, string sender, string[] toAddressList, string[]? ccAddressList = null, string[]? bccAddressList = null);
        Task SendMailAsync(bool containsHtml, string subject, string body, string? footer, string sender, string[] toAddressList, string[]? ccAddressList = null, string[]? bccAddressList = null);
    }
}
