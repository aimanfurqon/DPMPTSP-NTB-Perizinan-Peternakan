using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Threading.Tasks;

namespace PerizinanPeternakan.Services
{
    // Kelas untuk menampung konfigurasi SMTP dari user secrets
    public class SmtpOptions
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string AppPassword { get; set; }
    }

    // Ganti nama kelas dari EmailSender menjadi MailKitEmailSender agar lebih jelas
    public class MailKitEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;

        public MailKitEmailSender(IOptions<SmtpOptions> optionsAccessor)
        {
            _options = optionsAccessor.Value;
        }

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

                System.Diagnostics.Debug.WriteLine($"Email ke {toEmail} berhasil dikirim melalui MailKit!");
            }
            catch (System.Exception ex)
            {
                // Jika gagal, tampilkan pesan error di console debug
                System.Diagnostics.Debug.WriteLine($"Gagal mengirim email ke {toEmail}. Error: {ex.Message}");
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
