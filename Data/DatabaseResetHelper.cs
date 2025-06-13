using Microsoft.EntityFrameworkCore;

namespace PerizinanPeternakan.Data
{
    public static class DatabaseResetHelper
    {
        public static void ResetAndSeedDatabase(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Starting database reset and seed...");

                // Hapus database yang lama
                context.Database.EnsureDeleted();
                logger.LogInformation("Old database deleted");

                // Buat database baru
                context.Database.EnsureCreated();
                logger.LogInformation("New database created");

                // Seed data
                SeedDataHelper.SeedDummyData(context);
                logger.LogInformation("Database seeded successfully");

                logger.LogInformation("Database reset and seed completed!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during database reset and seed");
                throw;
            }
        }

        public static void ForceReseedUsers(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Force reseeding users...");

                // Hapus semua user yang ada (kecuali yang sudah punya foreign key constraints)
                var existingUsers = context.Users.ToList();

                // Hapus history approval dulu (karena ada foreign key ke users)
                var histories = context.PermitApprovalHistories.ToList();
                context.PermitApprovalHistories.RemoveRange(histories);

                // Hapus permits (karena ada foreign key ke users)
                var permits = context.PermitApplications.ToList();
                context.PermitApplications.RemoveRange(permits);

                // Hapus livestock details
                var livestock = context.LivestockDetails.ToList();
                context.LivestockDetails.RemoveRange(livestock);

                // Hapus permit documents
                var documents = context.PermitDocuments.ToList();
                context.PermitDocuments.RemoveRange(documents);

                // Hapus users
                context.Users.RemoveRange(existingUsers);

                context.SaveChanges();
                logger.LogInformation("Existing data cleared");

                // Seed ulang semua data
                SeedAllData(context);

                logger.LogInformation("Force reseed completed!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during force reseed");
                throw;
            }
        }

        private static void SeedAllData(ApplicationDbContext context)
        {
            // Seed users
            var users = new[]
            {
                new Models.User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@dpmptsp-ntb.go.id",
                    Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    NamaLengkap = "Hj. Eva Dewiyani, SP",
                    NoTelepon = "081234567890",
                    Alamat = "Kantor DPMPTSP NTB, Jl. Udayana No. 4 Mataram",
                    Role = "KepalaDinas",
                    TanggalDaftar = new DateTime(2024, 1, 1),
                    IsActive = true
                },
                new Models.User
                {
                    Id = 2,
                    Username = "verifikator1",
                    Email = "verifikator1@dpmptsp-ntb.go.id",
                    Password = BCrypt.Net.BCrypt.HashPassword("verifikator123"),
                    NamaLengkap = "Ahmad Verifikasi, S.Pt",
                    NoTelepon = "081234567891",
                    Alamat = "Kantor DPMPTSP NTB, Jl. Udayana No. 4 Mataram",
                    Role = "Verifikator",
                    TanggalDaftar = new DateTime(2024, 1, 15),
                    IsActive = true
                },
                new Models.User
                {
                    Id = 3,
                    Username = "verifikator2",
                    Email = "verifikator2@dpmptsp-ntb.go.id",
                    Password = BCrypt.Net.BCrypt.HashPassword("verifikator123"),
                    NamaLengkap = "Siti Verifikasi, S.Pt",
                    NoTelepon = "081234567893",
                    Alamat = "Kantor DPMPTSP NTB, Jl. Udayana No. 4 Mataram",
                    Role = "Verifikator",
                    TanggalDaftar = new DateTime(2024, 1, 20),
                    IsActive = true
                },
                new Models.User
                {
                    Id = 4,
                    Username = "user1",
                    Email = "user1@example.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("user123"),
                    NamaLengkap = "Budi Peternak",
                    NoTelepon = "081234567892",
                    Alamat = "Desa Suka Maju, Kec. Praya, Lombok Tengah",
                    Role = "User",
                    TanggalDaftar = new DateTime(2024, 2, 1),
                    IsActive = true
                },
                new Models.User
                {
                    Id = 5,
                    Username = "cvdena",
                    Email = "cvdena@example.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("cvdena123"),
                    NamaLengkap = "CV. DENA BERSAUDARA",
                    NoTelepon = "081234567894",
                    Alamat = "Desa Dena, Kec. Madapangga, Kab. Bima",
                    Role = "User",
                    TanggalDaftar = new DateTime(2024, 3, 1),
                    IsActive = true
                },
                new Models.User
                {
                    Id = 6,
                    Username = "sarimakmur",
                    Email = "sarimakmur@example.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("sari123"),
                    NamaLengkap = "PT. Sari Makmur Ternak",
                    NoTelepon = "081234567895",
                    Alamat = "Jl. Peternakan No. 15, Mataram",
                    Role = "User",
                    TanggalDaftar = new DateTime(2024, 3, 15),
                    IsActive = true
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();

            // Seed dummy permits
            SeedDataHelper.SeedDummyData(context);
        }
    }
}