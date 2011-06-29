using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;
using System.Configuration;

namespace OSBLE.Utility
{
    public static class Email
    {
        public static void Send(string subject, string message, IEnumerable<MailAddress> to)
        {
#if !DEBUG
            SmtpClient mailClient = new SmtpClient();
            mailClient.UseDefaultCredentials = true;

            MailAddress toFrom = new MailAddress(ConfigurationManager.AppSettings["OSBLEFromEmail"], "OSBLE");
            MailMessage mm = new MailMessage();

            mm.From = toFrom;
            mm.To.Add(toFrom);
            mm.Subject = subject;
            mm.Body = message;

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