using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Models;

namespace PerizinanPeternakan.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Port> Ports { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<LivestockPermitApplication> PermitApplications { get; set; }
        public DbSet<LivestockDetail> LivestockDetails { get; set; }
        public DbSet<PermitApprovalHistory> PermitApprovalHistories { get; set; }
        public DbSet<PermitDocument> PermitDocuments { get; set; }

        public DbSet<LivestockQuota> LivestockQuotas { get; set; }
        public DbSet<QuotaUsage> QuotaUsages { get; set; }


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

                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

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

                entity.Property(e => e.OriginProvinceId).IsRequired(false);
                entity.Property(e => e.OriginRegencyId).IsRequired(false);
                entity.Property(e => e.DestinationProvinceId).IsRequired(false);
                entity.Property(e => e.DestinationRegencyId).IsRequired(false);

                entity.HasOne(d => d.User)
                      .WithMany()
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Admin)
                      .WithMany()
                      .HasForeignKey(d => d.AdminId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull); 

                entity.HasOne(d => d.Verifikator)
                      .WithMany()
                      .HasForeignKey(d => d.VerifikatorId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull); 

                entity.HasOne(d => d.KepalaDinas)
                      .WithMany()
                      .HasForeignKey(d => d.KepalaDinasId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull); 

                entity.HasIndex(e => e.ApplicationNumber).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.SubmissionDate);
                entity.HasIndex(e => e.OriginProvinceId);
                entity.HasIndex(e => e.OriginRegencyId);
                entity.HasIndex(e => e.DestinationProvinceId);
                entity.HasIndex(e => e.DestinationRegencyId);
            });
            
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
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => e.ActionDate);
            });

      
            modelBuilder.Entity<PermitDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DocumentName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.DocumentType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FileExtension).HasMaxLength(10);

                // NEW: Document details configuration
                entity.Property(e => e.DocumentDate)
                      .HasColumnType("datetime2")
                      .IsRequired(false);

                entity.Property(e => e.DocumentNumber)
                      .HasMaxLength(50)
                      .IsRequired(false);

                entity.Property(e => e.DocumentDescription)
                      .HasMaxLength(500)
                      .IsRequired(false);

                entity.HasOne(d => d.PermitApplication)
                      .WithMany(p => p.Documents)
                      .HasForeignKey(d => d.PermitApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.UploadedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.UploadedByUserId)
                      .OnDelete(DeleteBehavior.NoAction);

               
                entity.HasIndex(e => e.DocumentDate);
                entity.HasIndex(e => e.DocumentNumber);
                entity.HasIndex(e => e.DocumentType);
            });
            modelBuilder.Entity<LivestockQuota>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LivestockType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ProvinceCode).IsRequired().HasMaxLength(5);
                entity.Property(e => e.ProvinceName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Year).IsRequired();
                entity.Property(e => e.TotalQuota).IsRequired();
                entity.Property(e => e.UsedQuota).IsRequired();
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.RegulationReference).HasMaxLength(100);

                // Indexes untuk performance
                entity.HasIndex(e => new { e.LivestockType, e.ProvinceCode, e.Year })
                      .IsUnique()
                      .HasDatabaseName("IX_LivestockQuotas_Type_Province_Year");
                entity.HasIndex(e => e.ProvinceCode);
                entity.HasIndex(e => e.Year);
            });

            // QuotaUsage Configuration
            modelBuilder.Entity<QuotaUsage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Notes).HasMaxLength(200);

                // Foreign key relationships
                entity.HasOne(e => e.LivestockQuota)
                      .WithMany(lq => lq.QuotaUsages)
                      .HasForeignKey(e => e.LivestockQuotaId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PermitApplication)
                      .WithMany()
                      .HasForeignKey(e => e.PermitApplicationId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Indexes
                entity.HasIndex(e => new { e.LivestockQuotaId, e.Status });
                entity.HasIndex(e => e.PermitApplicationId);
            });

            // Data seeding untuk Port
            modelBuilder.Entity<Port>()
                .HasIndex(p => p.Code)
                .IsUnique();

            modelBuilder.Entity<Port>()
                .HasIndex(p => new { p.ProvinceCode, p.Name });

        }
    }
}
