using PerizinanPeternakan.Models;
using Microsoft.EntityFrameworkCore;

namespace PerizinanPeternakan.Data
{
    public static class SeedDataHelper
    {

        // Tambahkan di SeedDataHelper.cs untuk menambahkan lebih banyak data AdminHistory

        public static void AddMoreAdminHistoryData(ApplicationDbContext context)
        {
            try
            {
                // Cek apakah sudah ada data
                var existingHistoryCount = context.PermitApprovalHistories.Count();

                if (existingHistoryCount < 20) // Jika data masih sedikit
                {
                    var additionalHistories = new List<PermitApprovalHistory>();

                    // Data tambahan untuk admin history
                    var additionalAdminHistories = new[]
                    {
                new {
                    PermitId = 1,
                    AdminId = 2, // admin1
                    Action = "Disetujui Admin",
                    Comments = "Data permohonan sudah lengkap dan sesuai persyaratan. PDF dokumen telah digenerate untuk tahap verifikasi selanjutnya.",
                    ActionDate = DateTime.Now.AddDays(-15)
                },
                new {
                    PermitId = 2,
                    AdminId = 2, // admin1
                    Action = "Disetujui Admin",
                    Comments = "Review data selesai. Semua dokumen pendukung telah diverifikasi. PDF siap untuk verifikasi lapangan.",
                    ActionDate = DateTime.Now.AddDays(-12)
                },
                new {
                    PermitId = 3,
                    AdminId = 3, // admin2
                    Action = "Disetujui Admin",
                    Comments = "Data telah direview dan disetujui. PDF dokumen siap untuk diverifikasi.",
                    ActionDate = DateTime.Now.AddDays(-8)
                },
                new {
                    PermitId = 4,
                    AdminId = 2, // admin1
                    Action = "Disetujui Admin",
                    Comments = "Permohonan untuk CV. Maju Sejahtera telah direview. Data lengkap dan valid.",
                    ActionDate = DateTime.Now.AddDays(-3)
                }
            };

                    foreach (var history in additionalAdminHistories)
                    {
                        // Cek apakah history untuk permit ini sudah ada
                        var existingHistory = context.PermitApprovalHistories
                            .Any(h => h.PermitApplicationId == history.PermitId &&
                                     h.UserId == history.AdminId &&
                                     h.Action.Contains("Admin"));

                        if (!existingHistory)
                        {
                            var newHistory = new PermitApprovalHistory
                            {
                                PermitApplicationId = history.PermitId,
                                UserId = history.AdminId,
                                FromStatus = PermitStatus.Submitted,
                                ToStatus = PermitStatus.AdminApproved,
                                Action = history.Action,
                                Comments = history.Comments,
                                ActionDate = history.ActionDate
                            };

                            additionalHistories.Add(newHistory);
                        }
                    }

                    if (additionalHistories.Any())
                    {
                        context.PermitApprovalHistories.AddRange(additionalHistories);
                        context.SaveChanges();
                        Console.WriteLine($"Added {additionalHistories.Count} additional admin history records");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding admin history data: {ex.Message}");
            }
        }

        // Method untuk menambahkan history admin secara individual
        public static async Task<bool> AddAdminHistoryRecord(ApplicationDbContext context,
            int permitId, int adminId, string action, string comments = null)
        {
            try
            {
                // Validasi permit exists
                var permit = await context.PermitApplications.FindAsync(permitId);
                if (permit == null) return false;

                // Validasi admin exists
                var admin = await context.Users.FindAsync(adminId);
                if (admin == null || admin.Role != "Admin") return false;

                // Tentukan status berdasarkan action
                PermitStatus fromStatus = PermitStatus.Submitted;
                PermitStatus toStatus = action.Contains("Disetujui") ?
                    PermitStatus.AdminApproved : PermitStatus.AdminRejected;

                var history = new PermitApprovalHistory
                {
                    PermitApplicationId = permitId,
                    UserId = adminId,
                    FromStatus = fromStatus,
                    ToStatus = toStatus,
                    Action = action,
                    Comments = comments ?? $"Review admin untuk permohonan {permit.ApplicationNumber}",
                    ActionDate = DateTime.Now
                };

                context.PermitApprovalHistories.Add(history);

                // Update permit status dan admin info
                permit.Status = toStatus;
                permit.AdminId = adminId;
                permit.AdminApprovalDate = DateTime.Now;

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding admin history: {ex.Message}");
                return false;
            }
        }

        // Method untuk bulk insert admin history (untuk testing)
        public static void SeedBulkAdminHistory(ApplicationDbContext context)
        {
            var random = new Random();
            var comments = new[]
            {
        "Data permohonan lengkap dan sesuai persyaratan. Dokumen pendukung telah diverifikasi.",
        "Review data selesai. PDF dokumen telah digenerate untuk tahap verifikasi selanjutnya.",
        "Permohonan disetujui setelah review menyeluruh. Semua dokumen valid.",
        "Data pemohon dan perusahaan telah diverifikasi. Status permohonan disetujui.",
        "Review administrasi selesai. Permohonan memenuhi semua kriteria yang dipersyaratkan.",
        "Dokumen lengkap dan data valid. Permohonan dapat dilanjutkan ke tahap verifikasi.",
        "Setelah review data, permohonan disetujui untuk proses selanjutnya.",
        "Data dan dokumen pendukung telah direview dan dinyatakan sesuai.",
        "Review administrasi completed. PDF izin telah digenerate.",
        "Permohonan disetujui admin. Data lengkap dan memenuhi persyaratan."
    };

            // Buat 10 contoh history admin tambahan
            for (int i = 1; i <= 10; i++)
            {
                var adminId = random.Next(2, 4); // admin1 atau admin2 (ID 2 atau 3)
                var comment = comments[random.Next(comments.Length)];
                var daysAgo = random.Next(1, 30);

                var history = new PermitApprovalHistory
                {
                    PermitApplicationId = i <= 4 ? i : random.Next(1, 5), // Gunakan permit yang ada
                    UserId = adminId,
                    FromStatus = PermitStatus.Submitted,
                    ToStatus = PermitStatus.AdminApproved,
                    Action = "Disetujui Admin",
                    Comments = comment,
                    ActionDate = DateTime.Now.AddDays(-daysAgo).AddHours(random.Next(8, 17))
                };

                // Cek duplikasi
                var exists = context.PermitApprovalHistories.Any(h =>
                    h.PermitApplicationId == history.PermitApplicationId &&
                    h.UserId == history.UserId &&
                    h.Action == history.Action);

                if (!exists)
                {
                    context.PermitApprovalHistories.Add(history);
                }
            }

            context.SaveChanges();
        }

        public static void SeedDummyData(ApplicationDbContext context)
        {
            // Cek apakah sudah ada data perizinan
            if (context.PermitApplications.Any())
            {
                return; // Sudah ada data, tidak perlu seed
            }

            // Seed data perizinan dummy untuk testing dengan flow baru
            var permitApplications = new List<LivestockPermitApplication>
            {
                // 1. Permohonan sudah selesai (FinalApproved)
                new LivestockPermitApplication
                {
                    Id = 1,
                    ApplicationNumber = "001/03-260/DPM&PTSP/2024",
                    UserId = 7, // cvdena
                    CompanyName = "CV. DENA BERSAUDARA",
                    CompanyAddress = "Desa Dena, Kec. Madapangga, Kab. Bima",
                    OriginLocation = "Kab. Bima",
                    DestinationLocation = "Kab. Jeneponto, Sulawesi Selatan",
                    DeparturePort = "Pelabuhan Sape, Kab. Bima",
                    ArrivalPort = "Pelabuhan Bungeng, Kab. Jeneponto",
                    Status = PermitStatus.FinalApproved,
                    SubmissionDate = new DateTime(2024, 6, 6),
                    AdminApprovalDate = new DateTime(2024, 6, 8),
                    VerificationDate = new DateTime(2024, 6, 10),
                    FinalApprovalDate = new DateTime(2024, 6, 11),
                    ValidFrom = new DateTime(2024, 6, 11),
                    ValidUntil = new DateTime(2024, 12, 11),
                    AdminId = 2,
                    VerifikatorId = 4,
                    KepalaDinasId = 1,
                    CurrentApprovalLevel = 4,
                    GeneratedDocumentPath = "/documents/permits/permit_001_03-260_DPM&PTSP_2024.pdf"
                },
                
                // 2. Permohonan sedang menunggu Kepala Dinas (Level 3)
                new LivestockPermitApplication
                {
                    Id = 2,
                    ApplicationNumber = "002/03-260/DPM&PTSP/2024",
                    UserId = 6, // user1
                    CompanyName = "UD. Budi Makmur",
                    CompanyAddress = "Desa Suka Maju, Kec. Praya, Lombok Tengah",
                    OriginLocation = "Lombok Tengah",
                    DestinationLocation = "Makassar, Sulawesi Selatan",
                    DeparturePort = "Pelabuhan Lembar, Lombok Barat",
                    ArrivalPort = "Pelabuhan Makassar",
                    Status = PermitStatus.VerifikatorApproved,
                    SubmissionDate = new DateTime(2024, 6, 12),
                    AdminApprovalDate = new DateTime(2024, 6, 13),
                    VerificationDate = new DateTime(2024, 6, 14),
                    AdminId = 2,
                    VerifikatorId = 4,
                    CurrentApprovalLevel = 3,
                    GeneratedDocumentPath = "/documents/permits/permit_002_03-260_DPM&PTSP_2024.pdf"
                },
                
                // 3. Permohonan sedang diverifikasi (Level 2) - Ada PDF dari Admin
                new LivestockPermitApplication
                {
                    Id = 3,
                    ApplicationNumber = "003/03-260/DPM&PTSP/2024",
                    UserId = 8, // sarimakmur
                    CompanyName = "PT. Sari Makmur Ternak",
                    CompanyAddress = "Jl. Peternakan No. 15, Mataram",
                    OriginLocation = "Mataram",
                    DestinationLocation = "Surabaya, Jawa Timur",
                    DeparturePort = "Pelabuhan Lembar, Lombok Barat",
                    ArrivalPort = "Pelabuhan Tanjung Perak, Surabaya",
                    Status = PermitStatus.AdminApproved,
                    SubmissionDate = DateTime.Now.AddDays(-3),
                    AdminApprovalDate = DateTime.Now.AddDays(-1),
                    AdminId = 3,
                    CurrentApprovalLevel = 2,
                    GeneratedDocumentPath = "/documents/permits/permit_003_03-260_DPM&PTSP_2024.pdf"
                },

                // 4. Permohonan baru masuk (butuh review admin - Level 1)
                new LivestockPermitApplication
                {
                    Id = 4,
                    ApplicationNumber = "004/03-260/DPM&PTSP/2024",
                    UserId = 6, // user1
                    CompanyName = "CV. Maju Sejahtera",
                    CompanyAddress = "Desa Maju, Kec. Gerung, Lombok Barat",
                    OriginLocation = "Lombok Barat",
                    DestinationLocation = "Denpasar, Bali",
                    DeparturePort = "Pelabuhan Lembar, Lombok Barat",
                    ArrivalPort = "Pelabuhan Benoa, Bali",
                    Status = PermitStatus.Submitted,
                    SubmissionDate = DateTime.Now.AddHours(-6),
                    CurrentApprovalLevel = 1
                }
            };

            context.PermitApplications.AddRange(permitApplications);

            // Seed livestock details
            var livestockDetails = new List<LivestockDetail>
            {
                // Untuk permohonan 1 (CV DENA)
                new LivestockDetail
                {
                    Id = 1,
                    PermitApplicationId = 1,
                    LivestockType = "Kuda Pedaging",
                    Quantity = 110,
                    Description = "Kuda pedaging berkualitas untuk perdagangan antar pulau"
                },
                
                // Untuk permohonan 2 (Budi Makmur)
                new LivestockDetail
                {
                    Id = 2,
                    PermitApplicationId = 2,
                    LivestockType = "Sapi Potong",
                    Quantity = 25,
                    Description = "Sapi Bali siap potong"
                },
                new LivestockDetail
                {
                    Id = 3,
                    PermitApplicationId = 2,
                    LivestockType = "Kerbau Potong",
                    Quantity = 15,
                    Description = "Kerbau Lumpur lokal"
                },
                
                // Untuk permohonan 3 (Sari Makmur)
                new LivestockDetail
                {
                    Id = 4,
                    PermitApplicationId = 3,
                    LivestockType = "Sapi Potong",
                    Quantity = 50,
                    Description = "Sapi Brahman Cross grade A"
                },
                new LivestockDetail
                {
                    Id = 5,
                    PermitApplicationId = 3,
                    LivestockType = "Kambing",
                    Quantity = 100,
                    Description = "Kambing Boer untuk breeding"
                },

                // Untuk permohonan 4 (CV Maju Sejahtera)
                new LivestockDetail
                {
                    Id = 6,
                    PermitApplicationId = 4,
                    LivestockType = "Sapi Potong",
                    Quantity = 30,
                    Description = "Sapi Bali lokal"
                }
            };

            context.LivestockDetails.AddRange(livestockDetails);

            // Seed approval history dengan flow baru
            var approvalHistories = new List<PermitApprovalHistory>
            {
                // History untuk permohonan 1 (sudah selesai - 4 level)
                new PermitApprovalHistory
                {
                    Id = 1,
                    PermitApplicationId = 1,
                    UserId = 7,
                    FromStatus = PermitStatus.Draft,
                    ToStatus = PermitStatus.Submitted,
                    Action = "Submit",
                    Comments = "Permohonan diajukan untuk CV. DENA BERSAUDARA",
                    ActionDate = new DateTime(2024, 6, 6, 10, 30, 0)
                },
                new PermitApprovalHistory
                {
                    Id = 2,
                    PermitApplicationId = 1,
                    UserId = 2, // admin1
                    FromStatus = PermitStatus.Submitted,
                    ToStatus = PermitStatus.AdminApproved,
                    Action = "Disetujui Admin",
                    Comments = "Data permohonan lengkap dan sesuai. PDF dokumen telah digenerate.",
                    ActionDate = new DateTime(2024, 6, 8, 14, 15, 0)
                },
                new PermitApprovalHistory
                {
                    Id = 3,
                    PermitApplicationId = 1,
                    UserId = 4, // verifikator1
                    FromStatus = PermitStatus.AdminApproved,
                    ToStatus = PermitStatus.VerifikatorApproved,
                    Action = "Disetujui Verifikator",
                    Comments = "Dokumen PDF telah diverifikasi dan sesuai dengan data. Rekomendasi untuk disetujui.",
                    ActionDate = new DateTime(2024, 6, 10, 11, 20, 0)
                },
                new PermitApprovalHistory
                {
                    Id = 4,
                    PermitApplicationId = 1,
                    UserId = 1, // kepala dinas
                    FromStatus = PermitStatus.VerifikatorApproved,
                    ToStatus = PermitStatus.FinalApproved,
                    Action = "Disetujui Kepala Dinas",
                    Comments = "Permohonan disetujui. Izin pengeluaran ternak diterbitkan.",
                    ActionDate = new DateTime(2024, 6, 11, 9, 45, 0)
                },
                
                // History untuk permohonan 2 (sampai level 3)
                new PermitApprovalHistory
                {
                    Id = 5,
                    PermitApplicationId = 2,
                    UserId = 6,
                    FromStatus = PermitStatus.Draft,
                    ToStatus = PermitStatus.Submitted,
                    Action = "Submit",
                    Comments = "Permohonan izin untuk UD. Budi Makmur",
                    ActionDate = new DateTime(2024, 6, 12, 11, 0, 0)
                },
                new PermitApprovalHistory
                {
                    Id = 6,
                    PermitApplicationId = 2,
                    UserId = 2, // admin1
                    FromStatus = PermitStatus.Submitted,
                    ToStatus = PermitStatus.AdminApproved,
                    Action = "Disetujui Admin",
                    Comments = "Review data selesai. PDF dokumen telah digenerate untuk verifikasi.",
                    ActionDate = new DateTime(2024, 6, 13, 15, 30, 0)
                },
                new PermitApprovalHistory
                {
                    Id = 7,
                    PermitApplicationId = 2,
                    UserId = 4, // verifikator1
                    FromStatus = PermitStatus.AdminApproved,
                    ToStatus = PermitStatus.VerifikatorApproved,
                    Action = "Disetujui Verifikator",
                    Comments = "Verifikasi dokumen PDF selesai. Siap untuk persetujuan akhir.",
                    ActionDate = new DateTime(2024, 6, 14, 10, 15, 0)
                },
                
                // History untuk permohonan 3 (sampai level 2)
                new PermitApprovalHistory
                {
                    Id = 8,
                    PermitApplicationId = 3,
                    UserId = 8,
                    FromStatus = PermitStatus.Draft,
                    ToStatus = PermitStatus.Submitted,
                    Action = "Submit",
                    Comments = "Permohonan izin untuk PT. Sari Makmur Ternak",
                    ActionDate = DateTime.Now.AddDays(-3).AddHours(9)
                },
                new PermitApprovalHistory
                {
                    Id = 9,
                    PermitApplicationId = 3,
                    UserId = 3, // admin2
                    FromStatus = PermitStatus.Submitted,
                    ToStatus = PermitStatus.AdminApproved,
                    Action = "Disetujui Admin",
                    Comments = "Data telah direview dan disetujui. PDF dokumen siap untuk diverifikasi.",
                    ActionDate = DateTime.Now.AddDays(-1).AddHours(14)
                },
                
                // History untuk permohonan 4 (baru submit)
                new PermitApprovalHistory
                {
                    Id = 10,
                    PermitApplicationId = 4,
                    UserId = 6,
                    FromStatus = PermitStatus.Draft,
                    ToStatus = PermitStatus.Submitted,
                    Action = "Submit",
                    Comments = "Permohonan izin untuk CV. Maju Sejahtera",
                    ActionDate = DateTime.Now.AddHours(-6)
                }
            };

            context.PermitApprovalHistories.AddRange(approvalHistories);

            // Save changes
            context.SaveChanges();
        }
    }
}