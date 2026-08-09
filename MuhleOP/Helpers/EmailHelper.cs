using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace MuhleOP.Helpers
{
    public static class EmailHelper
    {
        public static async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var emailUsername = WebConfigurationManager.AppSettings["EmailUsername"];
                var emailPassword = WebConfigurationManager.AppSettings["EmailPassword"];
                var emailHost = WebConfigurationManager.AppSettings["EmailHost"];
                var emailPort = int.Parse(WebConfigurationManager.AppSettings["EmailPort"]);
                var enableSsl = bool.Parse(WebConfigurationManager.AppSettings["EmailEnableSsl"]);
                var fromEmail = WebConfigurationManager.AppSettings["EmailFrom"];
                var fromName = WebConfigurationManager.AppSettings["EmailFromName"] ?? "MuhleOps";

                using (var client = new SmtpClient(emailHost, emailPort))
                {
                    client.EnableSsl = enableSsl;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(emailUsername, emailPassword);
                    client.Timeout = 30000; // 30 seconds

                    var mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress(fromEmail, fromName);
                    mailMessage.To.Add(toEmail);
                    mailMessage.Subject = subject;
                    mailMessage.Body = body;
                    mailMessage.IsBodyHtml = isHtml;

                    await client.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email error: {ex.Message}");
                return false;
            }
        }
    }
}