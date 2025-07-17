using System.Threading.Tasks;

namespace PerizinanPeternakan.Services
{
    // PENTING: Ini adalah implementasi DUMMY.
    // Anda HARUS menggantinya dengan servis email asli (misal: SendGrid, MailKit, SMTP).
    // Untuk sekarang, email hanya akan ditampilkan di console Debug.
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            System.Diagnostics.Debug.WriteLine("=====================================");
            System.Diagnostics.Debug.WriteLine($"To: {email}");
            System.Diagnostics.Debug.WriteLine($"Subject: {subject}");
            System.Diagnostics.Debug.WriteLine("Body:");
            System.Diagnostics.Debug.WriteLine(message);
            System.Diagnostics.Debug.WriteLine("=====================================");

            return Task.CompletedTask;
        }
    }
}
