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

            MailAddress fromAddress = new MailAddress(ConfigurationManager.AppSettings["OSBLEFromEmail"], "OSBLE");

            foreach (MailAddress recipient in to)
            {
                MailMessage mm = new MailMessage();

                mm.From = fromAddress;
                mm.To.Add(recipient);
                mm.Subject = subject;
                mm.Body = message;
                mm.IsBodyHtml = true;

                //bomb's away!
                mailClient.Send(mm);
            }
            
            mailClient.Dispose();
#endif
        }
    }
}