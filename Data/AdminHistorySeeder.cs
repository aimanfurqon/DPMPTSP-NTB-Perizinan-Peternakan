using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;

namespace PerizinanPeternakan.Data
{
    public static class AdminHistorySeeder
    {
        public static void SeedAdminHistoryData(ApplicationDbContext context)
        {
            try
            {
                // Cek apakah sudah ada data history admin yang cukup
                var existingAdminHistory = context.PermitApprovalHistories
                    .Where(h => h.Action.Contains("Admin"))
                    .Count();

                if (existingAdminHistory >= 10) return; // Skip jika sudah cukup

                var adminUsers = context.Users.Where(u => u.Role == "Admin").ToList();
                var permits = context.PermitApplications.Take(20).ToList();

                var sampleComments = new[]
                {
                    "Data permohonan telah direview dan dinyatakan lengkap. Semua dokumen pendukung sesuai dengan persyaratan yang berlaku.",
                    "Review administrasi selesai. PDF dokumen izin telah digenerate dan siap untuk tahap verifikasi selanjutnya.",
                    "Permohonan disetujui setelah verifikasi data pemohon dan kelengkapan dokumen. Status diubah menjadi approved.",
                    "Data perusahaan dan informasi ternak telah divalidasi. Permohonan memenuhi semua kriteria administratif.",
                    "Review data selesai dengan hasil memuaskan. Dokumen lengkap dan data ternak sesuai ketentuan.",
                    "Verifikasi administrasi completed. Semua persyaratan terpenuhi dan permohonan dapat dilanjutkan.",
                    "Data pemohon telah diverifikasi melalui sistem. Informasi perusahaan dan lokasi sudah sesuai.",
                    "Review dokumen pendukung selesai. Format dan kelengkapan dokumen memenuhi standar yang ditetapkan.",
                    "Permohonan disetujui berdasarkan kelengkapan data dan kesesuaian dengan regulasi yang berlaku.",
                    "Validasi data perusahaan dan ternak berhasil. PDF izin sementara telah digenerate untuk proses selanjutnya.",
                    "Data yang disubmit tidak lengkap. Beberapa dokumen pendukung masih kurang dan perlu dilengkapi.",
                    "Informasi lokasi asal ternak tidak sesuai dengan data di sistem. Permohonan perlu diperbaiki.",
                    "Format dokumen tidak sesuai standar. Mohon upload ulang dengan format yang benar."
                };

                var random = new Random(DateTime.Now.Millisecond);
                var historyList = new List<PermitApprovalHistory>();

                // Generate history untuk 15 permits
                for (int i = 0; i < Math.Min(15, permits.Count); i++)
                {
                    var permit = permits[i];
                    var admin = adminUsers[random.Next(adminUsers.Count)];
                    var isApproved = random.Next(100) < 85; // 85% chance approved
                    var daysAgo = random.Next(1, 60);
                    var hoursOffset = random.Next(8, 17); // Working hours

                    var history = new PermitApprovalHistory
                    {
                        PermitApplicationId = permit.Id,
                        UserId = admin.Id,
                        FromStatus = PermitStatus.Submitted,
                        ToStatus = isApproved ? PermitStatus.AdminApproved : PermitStatus.AdminRejected,
                        Action = isApproved ? "Disetujui Admin" : "Ditolak Admin",
                        Comments = isApproved ?
                            sampleComments[random.Next(0, 10)] :
                            sampleComments[random.Next(10, 13)],
                        ActionDate = DateTime.Now.AddDays(-daysAgo).AddHours(hoursOffset)
                    };

                    // Cek duplikasi
                    var exists = context.PermitApprovalHistories.Any(h =>
                        h.PermitApplicationId == history.PermitApplicationId &&
                        h.UserId == history.UserId &&
                        h.Action.Contains("Admin"));

                    if (!exists)
                    {
                        historyList.Add(history);

                        // Update permit data
                        permit.AdminId = admin.Id;
                        permit.AdminApprovalDate = history.ActionDate;
                        permit.Status = history.ToStatus;

                        if (!isApproved)
                        {
                            permit.RejectionReason = history.Comments;
                        }
                    }
                }

                if (historyList.Any())
                {
                    context.PermitApprovalHistories.AddRange(historyList);
                    context.SaveChanges();

                    Console.WriteLine($"✅ Added {historyList.Count} admin history records");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error seeding admin history: {ex.Message}");
            }
        }

        // Method untuk menambahkan single admin history record
        public static async Task<bool> AddSingleAdminHistory(
            ApplicationDbContext context,
            int permitId,
            int adminId,
            bool isApproved = true,
            string customComment = null)
        {
            try
            {
                var permit = await context.PermitApplications.FindAsync(permitId);
                var admin = await context.Users.FindAsync(adminId);

                if (permit == null || admin == null || admin.Role != "Admin")
                    return false;

                // Cek apakah sudah ada history admin untuk permit ini
                var existingHistory = await context.PermitApprovalHistories
                    .AnyAsync(h => h.PermitApplicationId == permitId &&
                                  h.UserId == adminId &&
                                  h.Action.Contains("Admin"));

                if (existingHistory) return false;

                var defaultComments = isApproved ?
                    "Data permohonan telah direview dan disetujui. PDF dokumen telah digenerate." :
                    "Permohonan ditolak karena data tidak lengkap atau tidak sesuai persyaratan.";

                var history = new PermitApprovalHistory
                {
                    PermitApplicationId = permitId,
                    UserId = adminId,
                    FromStatus = permit.Status,
                    ToStatus = isApproved ? PermitStatus.AdminApproved : PermitStatus.AdminRejected,
                    Action = isApproved ? "Disetujui Admin" : "Ditolak Admin",
                    Comments = customComment ?? defaultComments,
                    ActionDate = DateTime.Now
                };

                context.PermitApprovalHistories.Add(history);

                // Update permit
                permit.AdminId = adminId;
                permit.AdminApprovalDate = DateTime.Now;
                permit.Status = history.ToStatus;

                if (!isApproved)
                {
                    permit.RejectionReason = history.Comments;
                }

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding admin history: {ex.Message}");
                return false;
            }
        }

        // Method untuk generate history untuk testing dengan berbagai scenario
        public static void GenerateTestingAdminHistory(ApplicationDbContext context)
        {
            var scenarios = new[]
            {
                new { Action = "Disetujui Admin", Comment = "Data lengkap, dokumen sesuai, PDF generated", IsApproved = true },
                new { Action = "Disetujui Admin", Comment = "Review selesai, semua persyaratan terpenuhi", IsApproved = true },
                new { Action = "Disetujui Admin", Comment = "Verifikasi data sukses, lanjut ke tahap verifikator", IsApproved = true },
                new { Action = "Ditolak Admin", Comment = "Dokumen tidak lengkap, harap upload ulang", IsApproved = false },
                new { Action = "Ditolak Admin", Comment = "Data perusahaan tidak valid", IsApproved = false },
                new { Action = "Disetujui Admin", Comment = "Fast track approval - dokumen premium", IsApproved = true },
                new { Action = "Disetujui Admin", Comment = "Regular review completed successfully", IsApproved = true }
            };

            var permits = context.PermitApplications.Take(scenarios.Length).ToList();
            var admins = context.Users.Where(u => u.Role == "Admin").ToList();
            var random = new Random();

            for (int i = 0; i < scenarios.Length && i < permits.Count; i++)
            {
                var scenario = scenarios[i];
                var permit = permits[i];
                var admin = admins[random.Next(admins.Count)];

                var history = new PermitApprovalHistory
                {
                    PermitApplicationId = permit.Id,
                    UserId = admin.Id,
                    FromStatus = PermitStatus.Submitted,
                    ToStatus = scenario.IsApproved ? PermitStatus.AdminApproved : PermitStatus.AdminRejected,
                    Action = scenario.Action,
                    Comments = scenario.Comment,
                    ActionDate = DateTime.Now.AddDays(-random.Next(1, 30)).AddHours(random.Next(8, 17))
                };

                // Cek duplikasi
                var exists = context.PermitApprovalHistories.Any(h =>
                    h.PermitApplicationId == permit.Id &&
                    h.UserId == admin.Id &&
                    h.Action.Contains("Admin"));

                if (!exists)
                {
                    context.PermitApprovalHistories.Add(history);

                    // Update permit
                    permit.AdminId = admin.Id;
                    permit.AdminApprovalDate = history.ActionDate;
                    permit.Status = history.ToStatus;

                    if (!scenario.IsApproved)
                    {
                        permit.RejectionReason = scenario.Comment;
                    }
                }
            }

            context.SaveChanges();
            Console.WriteLine($"✅ Generated testing admin history scenarios");
        }
    }
}