using System.Threading.Tasks;

namespace PerizinanPeternakan.Services
{
    // PENTING: Ini adalah implementasi DUMMY.
    // Anda HARUS menggantinya dengan servis email asli (misal: SendGrid, MailKit, SMTP).
    // Untuk sekarang, email hanya akan ditampilkan di console.
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            Console.WriteLine("=====================================");
            Console.WriteLine($"To: {email}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine("Body:");
            Console.WriteLine(message);
            Console.WriteLine("=====================================");

            return Task.CompletedTask;
        }
    }
}
