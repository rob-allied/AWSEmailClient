using System;
using System.Net.Mail;
using System.Linq;
using System.Net;

namespace AWSEmailClient
{
    class Program
    {
        static readonly string CONFIGURATION_SET = "Emails";
        static readonly string SMTPHost = "email-smtp.us-east-1.amazonaws.com";
        static readonly int SMTPPORT = 587;
        static readonly string SMTPUsername = "AKIAXXXXXXXXXXXXX";
        static readonly string SMTPPassword = "SKXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

        static void Main(string[] args)
        {
            SendMail( new string[] { "rob.schleinkofer@alliedpayment.com" }, null, "rob.schleinkofer@alliedpayment.com", null, null, null, "Email from AWS", "Testing new SES user keys", null, true, null, null);
        }

        public static void SendMail(string[] toAddresses,
    string[] toNames,
    string fromAddress,
    string fromDisplayName,
    string replyToName,
    string replyToAddress,
    string subject,
    string body,
    string[] ccAddresses,
    bool isHtml, string attachmentName, string attachmentContent)
        {
            try
            {
                MailMessage mailMessage = SetupMailMessage(fromAddress, fromDisplayName, subject, body, toAddresses, ccAddresses);

                if (attachmentContent != null && attachmentContent.Length > 0)
                {
                    mailMessage.Attachments.Add(Attachment.CreateAttachmentFromString(attachmentContent, "text/csv"));
                    mailMessage.Attachments.Last().ContentDisposition.FileName = attachmentName;
                }

                SmtpClient client = new SmtpClient(SMTPHost, SMTPPORT);
                client.Credentials = new NetworkCredential(SMTPUsername, SMTPPassword);
                client.EnableSsl = true;

                Console.WriteLine("Attempting to send email...");

                try
                {
                    client.Send(mailMessage);
                    Console.WriteLine("Email sent");
                }
                catch (SmtpFailedRecipientsException ex)
                {
                    for (int i = 0; i < ex.InnerExceptions.Length; i++)
                    {
                        SmtpStatusCode status = ex.InnerExceptions[i].StatusCode;

                        if (status == SmtpStatusCode.MailboxBusy ||
                            status == SmtpStatusCode.MailboxUnavailable)
                        {
                            Console.WriteLine("Delivery failed - retrying in 5 seconds.");
                            System.Threading.Thread.Sleep(5000);
                            client.Send(mailMessage);
                        }
                        else
                        {
                            Console.WriteLine("Failed to deliver message to {0}", ex.InnerExceptions[i].FailedRecipient);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception caught in RetryIfBusy(): {0}", ex.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("The email was not sent.");
                Console.WriteLine("Error message: " + ex.Message);
                throw new Exception("The email was not sent." + ex.Message, ex.InnerException);
            }
        }

        private static MailMessage SetupMailMessage(string fromAddress, string fromDisplayName, string subject, string body, string[] toAddresses, string[] ccAddresses)
        {
            MailMessage mailMessage = new MailMessage();

            mailMessage.IsBodyHtml = true;
            mailMessage.From = new MailAddress(fromAddress, fromDisplayName);
            mailMessage.Subject = subject;
            mailMessage.Body = body;
            mailMessage.Headers.Add("X-SES-CONFIGURATION-SET", CONFIGURATION_SET);
            mailMessage.Headers.Add("X-SES-MESSAGE-TAGS", "emailcategory = awsreporting");

            for (int i = 0; i < toAddresses.Count(); i++)
            {
                mailMessage.To.Add(new MailAddress(toAddresses[i]));
            }

            if (ccAddresses != null)
            {
                for (int i = 0; i < ccAddresses.Count(); i++)
                {
                    mailMessage.CC.Add(new MailAddress(ccAddresses[i]));
                }
            }

            return mailMessage;
        }
    }
}
