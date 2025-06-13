using PerizinanPeternakan.Models;
using Microsoft.EntityFrameworkCore;

namespace PerizinanPeternakan.Data
{
    public static class SeedDataHelper
    {
        public static void SeedDummyData(ApplicationDbContext context)
        {
            // Cek apakah sudah ada data perizinan
            if (context.PermitApplications.Any())
            {
                return; // Sudah ada data, tidak perlu seed
            }

            // Seed data perizinan dummy untuk testing
            var permitApplications = new List<LivestockPermitApplication>
            {
                // Permohonan sudah disetujui (untuk CV DENA BERSAUDARA)
                new LivestockPermitApplication
                {
                    Id = 1,
                    ApplicationNumber = "001/03-260/DPM&PTSP/2024",
                    UserId = 5, // cvdena
                    CompanyName = "CV. DENA BERSAUDARA",
                    CompanyAddress = "Desa Dena, Kec. Madapangga, Kab. Bima",
                    OriginLocation = "Kab. Bima",
                    DestinationLocation = "Kab. Jeneponto, Sulawesi Selatan",
                    DeparturePort = "Pelabuhan Sape, Kab. Bima",
                    ArrivalPort = "Pelabuhan Bungeng, Kab. Jeneponto",
                    Status = PermitStatus.FinalApproved,
                    SubmissionDate = new DateTime(2024, 6, 6),
                    VerificationDate = new DateTime(2024, 6, 10),
                    FinalApprovalDate = new DateTime(2024, 6, 11),
                    ValidFrom = new DateTime(2024, 6, 11),
                    ValidUntil = new DateTime(2024, 6, 25),
                    VerifikatorId = 2,
                    KepalaDinasId = 1,
                    CurrentApprovalLevel = 3,
                    GeneratedDocumentPath = "/documents/permits/permit_001_03-260_DPM&PTSP_2024.pdf"
                },
                
                // Permohonan sedang menunggu Kepala Dinas
                new LivestockPermitApplication
                {
                    Id = 2,
                    ApplicationNumber = "002/03-260/DPM&PTSP/2024",
                    UserId = 4, // user1
                    CompanyName = "UD. Budi Makmur",
                    CompanyAddress = "Desa Suka Maju, Kec. Praya, Lombok Tengah",
                    OriginLocation = "Lombok Tengah",
                    DestinationLocation = "Makassar, Sulawesi Selatan",
                    DeparturePort = "Pelabuhan Lembar, Lombok Barat",
                    ArrivalPort = "Pelabuhan Makassar",
                    Status = PermitStatus.VerifikatorApproved,
                    SubmissionDate = new DateTime(2024, 6, 12),
                    VerificationDate = new DateTime(2024, 6, 13),
                    VerifikatorId = 2,
                    CurrentApprovalLevel = 2
                },
                
                // Permohonan baru masuk (butuh verifikasi)
                new LivestockPermitApplication
                {
                    Id = 3,
                    ApplicationNumber = "003/03-260/DPM&PTSP/2024",
                    UserId = 6, // sarimakmur
                    CompanyName = "PT. Sari Makmur Ternak",
                    CompanyAddress = "Jl. Peternakan No. 15, Mataram",
                    OriginLocation = "Mataram",
                    DestinationLocation = "Surabaya, Jawa Timur",
                    DeparturePort = "Pelabuhan Lembar, Lombok Barat",
                    ArrivalPort = "Pelabuhan Tanjung Perak, Surabaya",
                    Status = PermitStatus.Submitted,
                    SubmissionDate = DateTime.Now.AddDays(-2),
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
                }
            };

            context.LivestockDetails.AddRange(livestockDetails);

            // Seed approval history
            var approvalHistories = new List<PermitApprovalHistory>
            {
                // History untuk permohonan 1 (sudah selesai)
                new PermitApprovalHistory
                {
                    Id = 1,
                    PermitApplicationId = 1,
                    UserId = 5,
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
                    UserId = 2,
                    FromStatus = PermitStatus.Submitted,
                    ToStatus = PermitStatus.VerifikatorApproved,
                    Action = "Disetujui Verifikator",
                    Comments = "Dokumen lengkap dan sesuai persyaratan teknis. Rekomendasi untuk disetujui.",
                    ActionDate = new DateTime(2024, 6, 10, 14, 15, 0)
                },
                new PermitApprovalHistory
                {
                    Id = 3,
                    PermitApplicationId = 1,
                    UserId = 1,
                    FromStatus = PermitStatus.VerifikatorApproved,
                    ToStatus = PermitStatus.FinalApproved,
                    Action = "Disetujui Kepala Dinas",
                    Comments = "Permohonan disetujui. Dokumen izin telah diterbitkan.",
                    ActionDate = new DateTime(2024, 6, 11, 9, 45, 0)
                },
                
                // History untuk permohonan 2 (menunggu Kepala Dinas)
                new PermitApprovalHistory
                {
                    Id = 4,
                    PermitApplicationId = 2,
                    UserId = 4,
                    FromStatus = PermitStatus.Draft,
                    ToStatus = PermitStatus.Submitted,
                    Action = "Submit",
                    Comments = "Permohonan izin untuk UD. Budi Makmur",
                    ActionDate = new DateTime(2024, 6, 12, 11, 0, 0)
                },
                new PermitApprovalHistory
                {
                    Id = 5,
                    PermitApplicationId = 2,
                    UserId = 2,
                    FromStatus = PermitStatus.Submitted,
                    ToStatus = PermitStatus.VerifikatorApproved,
                    Action = "Disetujui Verifikator",
                    Comments = "Verifikasi dokumen selesai. Semua persyaratan terpenuhi.",
                    ActionDate = new DateTime(2024, 6, 13, 15, 30, 0)
                },
                
                // History untuk permohonan 3 (baru masuk)
                new PermitApprovalHistory
                {
                    Id = 6,
                    PermitApplicationId = 3,
                    UserId = 6,
                    FromStatus = PermitStatus.Draft,
                    ToStatus = PermitStatus.Submitted,
                    Action = "Submit",
                    Comments = "Permohonan izin untuk PT. Sari Makmur Ternak",
                    ActionDate = DateTime.Now.AddDays(-2).AddHours(9)
                }
            };

            context.PermitApprovalHistories.AddRange(approvalHistories);

            // Save changes
            context.SaveChanges();
        }
    }
}