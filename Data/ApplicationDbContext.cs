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

            // Seed data admin default
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@dpmptsp-ntb.go.id",
                    Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    NamaLengkap = "Administrator",
                    NoTelepon = "081234567890",
                    Alamat = "Kantor DPMPTSP NTB",
                    Role = "Admin",
                    TanggalDaftar = DateTime.Now
                }
            );
        }
    }
}