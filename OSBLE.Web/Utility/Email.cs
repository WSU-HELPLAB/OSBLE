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
            //we need to prevent this from crashing, if it crashes it breaks websocket feedposting
            try
            {
                //ignore empty sends
                if (to.Count == 0)
                {
                    return;
                }

                // replace all newline chars (since this is html)
                message = message.Replace("\n", "<br>");

                SmtpClient mailClient = new SmtpClient();

                MailAddress fromAddress = new MailAddress(ConfigurationManager.AppSettings["OSBLEFromEmail"], "OSBLE");

                foreach (MailAddress recipient in to)
                {
                    try
                    {
                        MailMessage mm = new MailMessage();

                        mm.From = fromAddress;
                        mm.To.Add(recipient);
                        mm.Subject = subject;
                        mm.Body = message;
                        mm.IsBodyHtml = true;

                        //TODO: possibly add file attachments!
                        //add file attachments here
                        //mm.Attachments
                        //consider adding a reply to for mail items
                        //mm.ReplyTo

                        //bomb's away!
                        mailClient.Send(mm);
                    }
                    catch (System.Exception ex)
                    {
                        throw new System.Exception(string.Format("Failed to send email for recipient: {0} ", recipient.Address.ToString()), ex);
                    }
                }

                mailClient.Dispose();
            }
            catch (System.Exception e)
            {
                //do nothing for now
            }
            
#endif
        }
    }
}