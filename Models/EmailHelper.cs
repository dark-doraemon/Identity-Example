using System.Net.Mail;
using System.Net;
namespace Identity.Models
{
    //class này dùng để gửi mã thông báo đến địa chỉ email đã đăng kí của người dùng
    public class EmailHelper
    {
        public bool SendEmailTwoFactorCode(string userEmail, string code)
        {
            //class MailMessage dùng để gửi mail from....to ....
            MailMessage mailMessage = new MailMessage();

            //người gửi email
            mailMessage.From = new MailAddress("thedotnetchannelsender22@gmail.com");

            //người nhận email(có thể có nhiều người nhận)
            mailMessage.To.Add(new MailAddress(userEmail));

            //Chủ đề và nội dung
            mailMessage.Subject = "Two Factor Code";
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = code;


            //gửi mail bằng cách sử dụng giao thức SMTP (Simple Mail Transfer Protocol)
            //class này cung cấp các thuộc tính và phương thức để kết nối tới máy chủ SMTP
            SmtpClient client = new SmtpClient("smtp.gmail.com");
            client.Credentials = new System.Net.NetworkCredential("thedotnetchannelsender22@gmail.com", "lgioehkvchemfkrw");
            client.Port = 587;
            client.EnableSsl = true;
            try
            {
                client.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // log exception
            }
            return false;
        }


   
        public bool SendEmail(string userEmail, string confirmationLink)
        {
            //class MailMessage dùng để chứa nội dụng email
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("thedotnetchannelsender22@gmail.com");
            mailMessage.To.Add(new MailAddress(userEmail));

            mailMessage.Subject = "Confirm your email";
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = confirmationLink; // nội dung là đường link xác nhận

            SmtpClient client = new SmtpClient();
            client.Credentials = new System.Net.NetworkCredential("thedotnetchannelsender22@gmail.com", "lgioehkvchemfkrw");
            client.Host = "smtp.gmail.com";
            client.Port = 587;
            client.EnableSsl = true;    

            try
            {
                client.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // log exception
            }
            return false;
        }
    }
}
