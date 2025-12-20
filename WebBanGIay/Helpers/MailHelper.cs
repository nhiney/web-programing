using System;
using System.Net;
using System.Net.Mail;
using System.Configuration;

namespace WebBanGIay.Helpers
{
    public class MailHelper
    {
        public static bool IsConfigured()
        {
            var fromEmailAddress = ConfigurationManager.AppSettings["FromEmailAddress"]?.ToString();
            var fromEmailPassword = ConfigurationManager.AppSettings["FromEmailPassword"]?.ToString();
            var enabledVal = ConfigurationManager.AppSettings["Enabled"];

            bool enabled = !string.IsNullOrEmpty(enabledVal) && bool.Parse(enabledVal);
            if (!enabled) return false;

            if (string.IsNullOrEmpty(fromEmailAddress) || fromEmailAddress.Contains("YOUR_EMAIL")) return false;
            if (string.IsNullOrEmpty(fromEmailPassword) || fromEmailPassword.Contains("YOUR_APP_PASSWORD")) return false;

            return true;
        }

        public static void SendMail(string toEmail, string subject, string content)
        {
            if (!IsConfigured())
            {
                throw new Exception("SMTP_NOT_CONFIGURED: Vui lòng cấu hình email chính xác trong Web.config.");
            }

            var fromEmailAddress = ConfigurationManager.AppSettings["FromEmailAddress"].ToString();
            var fromEmailDisplayName = ConfigurationManager.AppSettings["FromEmailDisplayName"].ToString();
            var fromEmailPassword = ConfigurationManager.AppSettings["FromEmailPassword"].ToString();
            var smtpHost = ConfigurationManager.AppSettings["SMTPHost"].ToString();
            var smtpPort = ConfigurationManager.AppSettings["SMTPPort"].ToString();

            string body = content;

            MailMessage message = new MailMessage(new MailAddress(fromEmailAddress, fromEmailDisplayName), new MailAddress(toEmail));
            message.Subject = subject;
            message.IsBodyHtml = true;
            message.Body = body;

            var client = new SmtpClient();
            client.Credentials = new NetworkCredential(fromEmailAddress, fromEmailPassword);
            client.Host = smtpHost;
            client.EnableSsl = true;
            client.Port = !string.IsNullOrEmpty(smtpPort) ? Convert.ToInt32(smtpPort) : 587;
            client.Send(message);
        }
    }
}
