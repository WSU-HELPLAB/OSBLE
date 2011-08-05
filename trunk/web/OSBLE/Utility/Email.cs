using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;

namespace OSBLE.Utility
{
    public static class Email
    {
        public static void Send(string subject, string message, ICollection<MailAddress> to)
        {
#if !DEBUG
            //ignore empty sends
            if (to.Count == 0)
            {
                return;
            }

            SmtpClient mailClient = new SmtpClient();
            mailClient.UseDefaultCredentials = true;

            MailAddress toFrom = new MailAddress(ConfigurationManager.AppSettings["OSBLEFromEmail"], "OSBLE");
            MailMessage mm = new MailMessage();

            mm.From = toFrom;
            mm.To.Add(toFrom);
            mm.Subject = subject;
            mm.Body = message;
            mm.IsBodyHtml = true;

            //add recipients
            foreach (MailAddress address in to)
            {
                mm.Bcc.Add(address);
            }

            //bomb's away!
            mailClient.Send(mm);
#endif
        }
    }
}