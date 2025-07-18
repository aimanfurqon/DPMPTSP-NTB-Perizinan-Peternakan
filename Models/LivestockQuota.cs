using System.ComponentModel.DataAnnotations;

namespace PerizinanPeternakan.Models
{
    public class LivestockQuota
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string LivestockType { get; set; } = string.Empty; // Sapi Potong, Kerbau Potong, dll

        [Required]
        [StringLength(5)]
        public string ProvinceCode { get; set; } = string.Empty; // Kode provinsi asal

        [Required]
        [StringLength(50)]
        public string ProvinceName { get; set; } = string.Empty; // Nama provinsi asal

        [Required]
        public int Year { get; set; } // Tahun kuota

        [Required]
        public int TotalQuota { get; set; } // Total kuota yang ditetapkan

        [Required]
        public int UsedQuota { get; set; } = 0; // Kuota yang sudah digunakan

        public int RemainingQuota => TotalQuota - UsedQuota; // Kuota yang tersisa

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; } // Catatan tambahan

        [StringLength(100)]
        public string? RegulationReference { get; set; } // Referensi regulasi (misal: SK Gubernur No. xxx)

        // Navigation properties untuk tracking penggunaan kuota
        public virtual ICollection<QuotaUsage> QuotaUsages { get; set; } = new List<QuotaUsage>();
    }

    public class QuotaUsage
    {
        public int Id { get; set; }

        [Required]
        public int LivestockQuotaId { get; set; }
        public virtual LivestockQuota LivestockQuota { get; set; }

        [Required]
        public int PermitApplicationId { get; set; }
        public virtual LivestockPermitApplication PermitApplication { get; set; }

        [Required]
        public int Quantity { get; set; } // Jumlah yang digunakan dari kuota

        public DateTime UsedAt { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string Status { get; set; } = "Reserved"; // Reserved, Confirmed, Cancelled

        [StringLength(200)]
        public string? Notes { get; set; }
    }

    // DTO untuk response API
    public class LivestockQuotaDto
    {
        public string LivestockType { get; set; } = string.Empty;
        public string ProvinceName { get; set; } = string.Empty;
        public int TotalQuota { get; set; }
        public int UsedQuota { get; set; }
        public int RemainingQuota { get; set; }
        public int Year { get; set; }
        public bool IsAvailable => RemainingQuota > 0;
        public double UsagePercentage { get; set; } // PERBAIKAN: Tambah setter
    }

    // DTO untuk validasi input quantity
    public class QuotaValidationRequest
    {
        [Required]
        public string LivestockType { get; set; } = string.Empty;

        [Required]
        public string ProvinceCode { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Jumlah harus lebih dari 0")]
        public int RequestedQuantity { get; set; }

        public int Year { get; set; } = DateTime.Now.Year;
    }

    public class QuotaValidationResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public int AvailableQuota { get; set; }
        public int TotalQuota { get; set; }
        public int UsedQuota { get; set; }
        public double UsagePercentage { get; set; }
        public string LivestockType { get; set; } = string.Empty;
        public string ProvinceName { get; set; } = string.Empty;
    }
}