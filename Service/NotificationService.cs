using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;

namespace PerizinanPeternakan.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IEmailSender emailSender,
            ApplicationDbContext context,
            ILogger<NotificationService> logger)
        {
            _emailSender = emailSender;
            _context = context;
            _logger = logger;
        }

        public async Task SendNewPermitNotificationAsync(LivestockPermitApplication permit, string userRole)
        {
            try
            {
                var recipients = await GetNotificationRecipientsAsync(userRole);
                
                foreach (var recipient in recipients)
                {
                    var subject = $"Permohonan Baru - {permit.ApplicationNumber}";
                    var message = GenerateNewPermitEmailMessage(permit, recipient, userRole);
                    
                    await _emailSender.SendEmailAsync(recipient.Email, subject, message);
                    
                    _logger.LogInformation("New permit notification sent to {Email} for permit {ApplicationNumber}", 
                        recipient.Email, permit.ApplicationNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending new permit notification for permit {ApplicationNumber}", permit.ApplicationNumber);
            }
        }

        public async Task SendApprovalNotificationAsync(LivestockPermitApplication permit, string action, string comments, string userRole)
        {
            try
            {
                var recipients = await GetNotificationRecipientsAsync(userRole);
                
                foreach (var recipient in recipients)
                {
                    var subject = $"Permohonan {action} - {permit.ApplicationNumber}";
                    var message = GenerateApprovalEmailMessage(permit, action, comments, recipient, userRole);
                    
                    await _emailSender.SendEmailAsync(recipient.Email, subject, message);
                    
                    _logger.LogInformation("Approval notification sent to {Email} for permit {ApplicationNumber}", 
                        recipient.Email, permit.ApplicationNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending approval notification for permit {ApplicationNumber}", permit.ApplicationNumber);
            }
        }

        public async Task SendRejectionNotificationAsync(LivestockPermitApplication permit, string comments, string userRole)
        {
            try
            {
                var subject = $"Permohonan Izin Ditolak - {permit.ApplicationNumber}";
                var body = GenerateRejectionEmailBody(permit, comments, userRole);
                var recipientEmail = permit.User.Email;

                await _emailSender.SendEmailAsync(recipientEmail, subject, body);
                _logger.LogInformation($"Rejection notification sent to {recipientEmail} for permit {permit.ApplicationNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send rejection notification for permit {permit.ApplicationNumber}");
            }
        }

        public async Task SendFinalApprovalNotificationAsync(LivestockPermitApplication permit, string comments)
        {
            try
            {
                var subject = $"Permohonan Izin Disetujui Final - {permit.ApplicationNumber}";
                var body = GenerateFinalApprovalEmailBody(permit, comments);
                var recipientEmail = permit.User.Email;

                await _emailSender.SendEmailAsync(recipientEmail, subject, body);
                _logger.LogInformation($"Final approval notification sent to {recipientEmail} for permit {permit.ApplicationNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send final approval notification for permit {permit.ApplicationNumber}");
            }
        }

        public async Task SendRejectionToPreviousLevelAsync(LivestockPermitApplication permit, string comments, string userRole)
        {
            try
            {
                var subject = $"Permohonan Izin Ditolak - {permit.ApplicationNumber}";
                var body = GenerateRejectionToPreviousLevelEmailBody(permit, comments, userRole);
                
                string recipientEmail = "";
                string recipientRole = "";

                // Determine recipient based on rejection level
                switch (userRole)
                {
                    case "Verifikator":
                        // Rejected by Verifikator, notify Admin
                        var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin" && u.IsActive);
                        if (admin != null)
                        {
                            recipientEmail = admin.Email;
                            recipientRole = "Admin";
                        }
                        break;
                    case "KepalaDinas":
                        // Rejected by KepalaDinas, notify Verifikator
                        var verifikator = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Verifikator" && u.IsActive);
                        if (verifikator != null)
                        {
                            recipientEmail = verifikator.Email;
                            recipientRole = "Verifikator";
                        }
                        break;
                }

                if (!string.IsNullOrEmpty(recipientEmail))
                {
                    await _emailSender.SendEmailAsync(recipientEmail, subject, body);
                    _logger.LogInformation($"Rejection to previous level notification sent to {recipientEmail} ({recipientRole}) for permit {permit.ApplicationNumber}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send rejection to previous level notification for permit {permit.ApplicationNumber}");
            }
        }

        private async Task<List<User>> GetNotificationRecipientsAsync(string userRole)
        {
            var recipients = new List<User>();

            switch (userRole)
            {
                case "Admin":
                    // Get all Admin users
                    recipients = await _context.Users
                        .Where(u => u.Role == "Admin" && u.IsActive)
                        .ToListAsync();
                    break;
                    
                case "Verifikator":
                    // Get all Verifikator users
                    recipients = await _context.Users
                        .Where(u => u.Role == "Verifikator" && u.IsActive)
                        .ToListAsync();
                    break;
                    
                case "KepalaDinas":
                    // Get all KepalaDinas users
                    recipients = await _context.Users
                        .Where(u => u.Role == "KepalaDinas" && u.IsActive)
                        .ToListAsync();
                    break;
            }

            return recipients;
        }

        private string GenerateNewPermitEmailMessage(LivestockPermitApplication permit, User recipient, string userRole)
        {
            var roleText = userRole switch
            {
                "Admin" => "Admin",
                "Verifikator" => "Verifikator",
                "KepalaDinas" => "Kepala Dinas",
                _ => userRole
            };

            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                            <h2 style='color: #007bff; margin-top: 0;'>🔔 Notifikasi Permohonan Baru</h2>
                            <p>Halo <strong>{recipient.NamaLengkap}</strong>,</p>
                            <p>Ada permohonan baru yang memerlukan review dari {roleText}.</p>
                        </div>

                        <div style='background-color: #ffffff; padding: 20px; border: 1px solid #dee2e6; border-radius: 8px; margin-bottom: 20px;'>
                            <h3 style='color: #28a745; margin-top: 0;'>📋 Detail Permohonan</h3>
                            <table style='width: 100%; border-collapse: collapse;'>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold; width: 150px;'>Nomor Permohonan:</td>
                                    <td style='padding: 8px;'>{permit.ApplicationNumber}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Pemohon:</td>
                                    <td style='padding: 8px;'>{permit.User.NamaLengkap}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Perusahaan:</td>
                                    <td style='padding: 8px;'>{permit.CompanyName}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Asal:</td>
                                    <td style='padding: 8px;'>{permit.OriginLocation}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Tujuan:</td>
                                    <td style='padding: 8px;'>{permit.DestinationLocation}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Tanggal Pengajuan:</td>
                                    <td style='padding: 8px;'>{permit.SubmissionDate:dd MMM yyyy HH:mm}</td>
                                </tr>
                            </table>
                        </div>

                        <div style='background-color: #e7f3ff; padding: 15px; border-radius: 8px; border-left: 4px solid #007bff;'>
                            <p style='margin: 0;'><strong>⚠️ Tindakan yang Diperlukan:</strong></p>
                            <p style='margin: 5px 0 0 0;'>Silakan login ke sistem untuk melakukan review terhadap permohonan ini.</p>
                        </div>

                        <div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6;'>
                            <p style='color: #6c757d; font-size: 14px; margin: 0;'>
                                Email ini dikirim secara otomatis oleh sistem DPMPTSP NTB
                            </p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerateApprovalEmailMessage(LivestockPermitApplication permit, string action, string comments, User recipient, string userRole)
        {
            var actionText = action == "Approve" ? "Disetujui" : "Ditolak";
            var roleText = userRole switch
            {
                "Admin" => "Admin",
                "Verifikator" => "Verifikator", 
                "KepalaDinas" => "Kepala Dinas",
                _ => userRole
            };

            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                            <h2 style='color: #007bff; margin-top: 0;'>✅ Notifikasi Approval</h2>
                            <p>Halo <strong>{recipient.NamaLengkap}</strong>,</p>
                            <p>Permohonan telah {actionText.ToLower()} oleh {roleText}.</p>
                        </div>

                        <div style='background-color: #ffffff; padding: 20px; border: 1px solid #dee2e6; border-radius: 8px; margin-bottom: 20px;'>
                            <h3 style='color: #28a745; margin-top: 0;'>📋 Detail Permohonan</h3>
                            <table style='width: 100%; border-collapse: collapse;'>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold; width: 150px;'>Nomor Permohonan:</td>
                                    <td style='padding: 8px;'>{permit.ApplicationNumber}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Pemohon:</td>
                                    <td style='padding: 8px;'>{permit.User.NamaLengkap}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Perusahaan:</td>
                                    <td style='padding: 8px;'>{permit.CompanyName}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Status:</td>
                                    <td style='padding: 8px;'>{PermitStatusHelper.GetStatusText(permit.Status)}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Aksi:</td>
                                    <td style='padding: 8px;'>{actionText}</td>
                                </tr>
                                {(comments != null ? $@"
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Komentar:</td>
                                    <td style='padding: 8px;'>{comments}</td>
                                </tr>" : "")}
                            </table>
                        </div>

                        <div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6;'>
                            <p style='color: #6c757d; font-size: 14px; margin: 0;'>
                                Email ini dikirim secara otomatis oleh sistem DPMPTSP NTB
                            </p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerateRejectionEmailMessage(LivestockPermitApplication permit, string comments, User recipient, string userRole)
        {
            var roleText = userRole switch
            {
                "Admin" => "Admin",
                "Verifikator" => "Verifikator",
                "KepalaDinas" => "Kepala Dinas", 
                _ => userRole
            };

            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background-color: #fff3cd; padding: 20px; border-radius: 8px; margin-bottom: 20px; border: 1px solid #ffeaa7;'>
                            <h2 style='color: #856404; margin-top: 0;'>⚠️ Notifikasi Penolakan</h2>
                            <p>Halo <strong>{recipient.NamaLengkap}</strong>,</p>
                            <p>Permohonan telah ditolak oleh {roleText} dan dikembalikan untuk review ulang.</p>
                        </div>

                        <div style='background-color: #ffffff; padding: 20px; border: 1px solid #dee2e6; border-radius: 8px; margin-bottom: 20px;'>
                            <h3 style='color: #dc3545; margin-top: 0;'>📋 Detail Permohonan</h3>
                            <table style='width: 100%; border-collapse: collapse;'>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold; width: 150px;'>Nomor Permohonan:</td>
                                    <td style='padding: 8px;'>{permit.ApplicationNumber}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Pemohon:</td>
                                    <td style='padding: 8px;'>{permit.User.NamaLengkap}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Perusahaan:</td>
                                    <td style='padding: 8px;'>{permit.CompanyName}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Status:</td>
                                    <td style='padding: 8px;'>{PermitStatusHelper.GetStatusText(permit.Status)}</td>
                                </tr>
                                {(comments != null ? $@"
                                <tr>
                                    <td style='padding: 8px; font-weight: bold;'>Alasan Penolakan:</td>
                                    <td style='padding: 8px;'>{comments}</td>
                                </tr>" : "")}
                            </table>
                        </div>

                        <div style='background-color: #f8d7da; padding: 15px; border-radius: 8px; border-left: 4px solid #dc3545;'>
                            <p style='margin: 0;'><strong>📝 Tindakan yang Diperlukan:</strong></p>
                            <p style='margin: 5px 0 0 0;'>Silakan login ke sistem untuk melakukan review ulang terhadap permohonan ini.</p>
                        </div>

                        <div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6;'>
                            <p style='color: #6c757d; font-size: 14px; margin: 0;'>
                                Email ini dikirim secara otomatis oleh sistem DPMPTSP NTB
                            </p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerateRejectionEmailBody(LivestockPermitApplication permit, string comments, string userRole)
        {
            var roleText = userRole switch
            {
                "Admin" => "Admin",
                "Verifikator" => "Verifikator",
                "KepalaDinas" => "Kepala Dinas",
                _ => userRole
            };

            return $@"
                <h2>Permohonan Izin Ditolak</h2>
                <p>Yth. {permit.User.NamaLengkap},</p>
                <p>Permohonan izin Anda dengan detail berikut telah ditolak:</p>
                <ul>
                    <li><strong>Nomor Permohonan:</strong> {permit.ApplicationNumber}</li>
                    <li><strong>Nama Perusahaan:</strong> {permit.CompanyName}</li>
                    <li><strong>Tanggal Pengajuan:</strong> {permit.SubmissionDate:dd MMM yyyy}</li>
                    <li><strong>Ditolak oleh:</strong> {roleText}</li>
                    <li><strong>Alasan Penolakan:</strong> {comments}</li>
                </ul>
                <p>Silakan perbaiki permohonan sesuai dengan alasan penolakan yang diberikan dan ajukan kembali.</p>
                <p>Terima kasih.</p>";
        }

        private string GenerateFinalApprovalEmailBody(LivestockPermitApplication permit, string comments)
        {
            return $@"
                <h2>Permohonan Izin Disetujui Final</h2>
                <p>Yth. {permit.User.NamaLengkap},</p>
                <p>Selamat! Permohonan izin Anda telah disetujui secara final oleh Kepala Dinas:</p>
                <ul>
                    <li><strong>Nomor Permohonan:</strong> {permit.ApplicationNumber}</li>
                    <li><strong>Nama Perusahaan:</strong> {permit.CompanyName}</li>
                    <li><strong>Tanggal Pengajuan:</strong> {permit.SubmissionDate:dd MMM yyyy}</li>
                    <li><strong>Tanggal Persetujuan:</strong> {permit.FinalApprovalDate:dd MMM yyyy}</li>
                    <li><strong>Berlaku dari:</strong> {permit.ValidFrom:dd MMM yyyy}</li>
                    <li><strong>Berlaku hingga:</strong> {permit.ValidUntil:dd MMM yyyy}</li>
                    <li><strong>Komentar:</strong> {comments}</li>
                </ul>
                <p>Izin Anda telah aktif dan dapat digunakan sesuai dengan ketentuan yang berlaku.</p>
                <p>Terima kasih.</p>";
        }

        private string GenerateRejectionToPreviousLevelEmailBody(LivestockPermitApplication permit, string comments, string userRole)
        {
            var roleText = userRole switch
            {
                "Verifikator" => "Verifikator",
                "KepalaDinas" => "Kepala Dinas",
                _ => userRole
            };

            var actionText = userRole switch
            {
                "Verifikator" => "Verifikator telah menolak permohonan ini dan mengembalikannya ke level Admin untuk review ulang.",
                "KepalaDinas" => "Kepala Dinas telah menolak permohonan ini dan mengembalikannya ke level Verifikator untuk review ulang.",
                _ => $"{roleText} telah menolak permohonan ini."
            };

            return $@"
                <h2>Permohonan Izin Ditolak - Perlu Review Ulang</h2>
                <p>Permohonan izin berikut telah ditolak oleh {roleText}:</p>
                <ul>
                    <li><strong>Nomor Permohonan:</strong> {permit.ApplicationNumber}</li>
                    <li><strong>Nama Perusahaan:</strong> {permit.CompanyName}</li>
                    <li><strong>Pemohon:</strong> {permit.User.NamaLengkap}</li>
                    <li><strong>Tanggal Pengajuan:</strong> {permit.SubmissionDate:dd MMM yyyy}</li>
                    <li><strong>Alasan Penolakan:</strong> {comments}</li>
                </ul>
                <p>{actionText}</p>
                <p>Silakan review kembali permohonan ini dan ambil tindakan yang sesuai.</p>";
        }
    }
} 