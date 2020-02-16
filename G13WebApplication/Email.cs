using System.Net;
using System.Net.Mail;


namespace G13WebApplication
{
    public class Email
    {
        public static void SendEmail(string strToEmail, string strSubject, string strBody)
        {
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com");
            smtpClient.UseDefaultCredentials = false;
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new NetworkCredential("g13esw1920@gmail.com", "123mejmo");

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress("g13esw1920@gmail.com")
            };

            mailMessage.To.Add(strToEmail);
            mailMessage.Body = strBody;
            mailMessage.Subject = strSubject;
            mailMessage.IsBodyHtml = true;
            smtpClient.Send(mailMessage);
        }
    }
}