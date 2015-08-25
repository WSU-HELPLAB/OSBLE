using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;

namespace OSBLE.Utility
{
    public static class Email
    {
        public static void Send(string subject, string message, ICollection<MailAddress> to)
        {

            //ignore empty sends
            if (to.Count == 0)
            {
                return;
            }

            SmtpClient mailClient = new SmtpClient();

            MailAddress fromAddress = new MailAddress(ConfigurationManager.AppSettings["OSBLEFromEmail"], "OSBLE");

            foreach (MailAddress recipient in to)
            {
                MailMessage mm = new MailMessage();

                mm.From = fromAddress;
                mm.To.Add(recipient);
                mm.Subject = subject;
                mm.Body = message;
                mm.IsBodyHtml = true;
#if !DEBUG
                //bomb's away!
                mailClient.Send(mm);
#endif
            }
            
            mailClient.Dispose();

        }
    }
}