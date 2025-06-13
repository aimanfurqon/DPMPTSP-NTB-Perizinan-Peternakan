using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Models;

namespace PerizinanPeternakan.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<LivestockPermitApplication> PermitApplications { get; set; }
        public DbSet<LivestockDetail> LivestockDetails { get; set; }
        public DbSet<PermitApprovalHistory> PermitApprovalHistories { get; set; }
        public DbSet<PermitDocument> PermitDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfigurasi tabel User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
                entity.Property(e => e.NamaLengkap).IsRequired().HasMaxLength(100);
                entity.Property(e => e.NoTelepon).HasMaxLength(20);
                entity.Property(e => e.Alamat).HasMaxLength(500);
                entity.Property(e => e.Role).HasMaxLength(20);

                // Index untuk username dan email (harus unik)
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Konfigurasi tabel LivestockPermitApplication
            modelBuilder.Entity<LivestockPermitApplication>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ApplicationNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CompanyAddress).IsRequired().HasMaxLength(500);
                entity.Property(e => e.OriginLocation).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DestinationLocation).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DeparturePort).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ArrivalPort).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RejectionReason).HasMaxLength(1000);
                entity.Property(e => e.GeneratedDocumentPath).HasMaxLength(500);

                // Relationships
                entity.HasOne(d => d.User)
                      .WithMany()
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Verifikator)
                      .WithMany()
                      .HasForeignKey(d => d.VerifikatorId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.KepalaDinas)
                      .WithMany()
                      .HasForeignKey(d => d.KepalaDinasId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Index
                entity.HasIndex(e => e.ApplicationNumber).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.SubmissionDate);
            });

            // Konfigurasi tabel LivestockDetail
            modelBuilder.Entity<LivestockDetail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LivestockType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(200);

                entity.HasOne(d => d.PermitApplication)
                      .WithMany(p => p.LivestockDetails)
                      .HasForeignKey(d => d.PermitApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Konfigurasi tabel PermitApprovalHistory
            modelBuilder.Entity<PermitApprovalHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Comments).HasMaxLength(1000);

                entity.HasOne(d => d.PermitApplication)
                      .WithMany(p => p.ApprovalHistory)
                      .HasForeignKey(d => d.PermitApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.User)
                      .WithMany()
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ActionDate);
            });

            // Konfigurasi tabel PermitDocument
            modelBuilder.Entity<PermitDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DocumentName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.DocumentType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FileExtension).HasMaxLength(10);

                entity.HasOne(d => d.PermitApplication)
                      .WithMany(p => p.Documents)
                      .HasForeignKey(d => d.PermitApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.UploadedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.UploadedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed data default users
            modelBuilder.Entity<User>().HasData(
                // Kepala Dinas (Admin)
                new User
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

                // Verifikator 1
                new User
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

                // Verifikator 2 (backup)
                new User
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

                // User Demo 1
                new User
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

                // User Demo 2 (CV/Perusahaan)
                new User
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

                // User Demo 3
                new User
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
            );
        }
    }
}