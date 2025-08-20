using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Threading.Tasks;

namespace PerizinanPeternakan.Services
{
    /// <summary>
    /// Configuration options for SMTP email settings.
    /// </summary>
    public class SmtpOptions
    {
        /// <summary>
        /// Gets or sets the SMTP host server.
        /// </summary>
        public string Host { get; set; }
        
        /// <summary>
        /// Gets or sets the SMTP port number.
        /// </summary>
        public int Port { get; set; }
        
        /// <summary>
        /// Gets or sets the SMTP username.
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Gets or sets the SMTP application password.
        /// </summary>
        public string AppPassword { get; set; }
    }

    /// <summary>
    /// Email sender service using MailKit for SMTP email delivery.
    /// </summary>
    public class MailKitEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;

        /// <summary>
        /// Initializes a new instance of the MailKitEmailSender class.
        /// </summary>
        /// <param name="optionsAccessor">The SMTP options accessor.</param>
        public MailKitEmailSender(IOptions<SmtpOptions> optionsAccessor)
        {
            _options = optionsAccessor.Value;
        }

        /// <summary>
        /// Sends an email asynchronously using MailKit SMTP client.
        /// </summary>
        /// <param name="toEmail">The recipient email address.</param>
        /// <param name="subject">The email subject.</param>
        /// <param name="htmlMessage">The HTML email message content.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            if (string.IsNullOrEmpty(_options.AppPassword))
            {
                throw new System.Exception("App Password untuk SMTP tidak ditemukan. Pastikan sudah diatur di User Secrets.");
            }

            // Membuat pesan email menggunakan MimeKit
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(_options.Username);
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlMessage
            };
            email.Body = builder.ToMessageBody();

            // Mengirim email menggunakan MailKit SmtpClient
            using var smtp = new SmtpClient();
            try
            {
                // Terhubung ke server SMTP Gmail dengan keamanan STARTTLS
                await smtp.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);

                // Autentikasi menggunakan username dan App Password
                await smtp.AuthenticateAsync(_options.Username, _options.AppPassword);

                // Kirim email
                await smtp.SendAsync(email);

                Console.WriteLine($"Email ke {toEmail} berhasil dikirim melalui MailKit!");
            }
            catch (System.Exception ex)
            {
                // Jika gagal, tampilkan pesan error di console
                Console.WriteLine($"Gagal mengirim email ke {toEmail}. Error: {ex.Message}");
                throw; // Lemparkan kembali error agar bisa ditangani lebih lanjut jika perlu
            }
            finally
            {
                // Putuskan koneksi dari server
                await smtp.DisconnectAsync(true);
            }
        }
    }
}
