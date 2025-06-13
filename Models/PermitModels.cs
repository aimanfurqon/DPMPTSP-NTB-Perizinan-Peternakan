using System.ComponentModel.DataAnnotations;

namespace PerizinanPeternakan.Models
{
    public class LivestockPermitApplication
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nomor permohonan harus diisi")]
        public string ApplicationNumber { get; set; } = string.Empty;

        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required(ErrorMessage = "Nama perusahaan harus diisi")]
        [StringLength(200, ErrorMessage = "Nama perusahaan maksimal 200 karakter")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Alamat perusahaan harus diisi")]
        [StringLength(500, ErrorMessage = "Alamat perusahaan maksimal 500 karakter")]
        public string CompanyAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Asal ternak harus diisi")]
        [StringLength(100, ErrorMessage = "Asal ternak maksimal 100 karakter")]
        public string OriginLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tujuan pengiriman harus diisi")]
        [StringLength(100, ErrorMessage = "Tujuan pengiriman maksimal 100 karakter")]
        public string DestinationLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pelabuhan asal harus diisi")]
        [StringLength(100, ErrorMessage = "Pelabuhan asal maksimal 100 karakter")]
        public string DeparturePort { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pelabuhan bongkar harus diisi")]
        [StringLength(100, ErrorMessage = "Pelabuhan bongkar maksimal 100 karakter")]
        public string ArrivalPort { get; set; } = string.Empty;

        public PermitStatus Status { get; set; } = PermitStatus.Submitted;

        public int CurrentApprovalLevel { get; set; } = 1;

        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidUntil { get; set; }

        public string? RejectionReason { get; set; }

        public int? VerifikatorId { get; set; }
        public virtual User? Verifikator { get; set; }

        public int? KepalaDinasId { get; set; }
        public virtual User? KepalaDinas { get; set; }

        public DateTime? VerificationDate { get; set; }

        public DateTime? FinalApprovalDate { get; set; }

        public string? GeneratedDocumentPath { get; set; }

        // Navigation properties
        public virtual ICollection<LivestockDetail> LivestockDetails { get; set; } = new List<LivestockDetail>();
        public virtual ICollection<PermitApprovalHistory> ApprovalHistory { get; set; } = new List<PermitApprovalHistory>();
        public virtual ICollection<PermitDocument> Documents { get; set; } = new List<PermitDocument>();
    }

    public class LivestockDetail
    {
        public int Id { get; set; }

        public int PermitApplicationId { get; set; }
        public virtual LivestockPermitApplication PermitApplication { get; set; }

        [Required(ErrorMessage = "Jenis ternak harus diisi")]
        [StringLength(50, ErrorMessage = "Jenis ternak maksimal 50 karakter")]
        public string LivestockType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jumlah ternak harus diisi")]
        [Range(1, 10000, ErrorMessage = "Jumlah ternak harus antara 1-10000 ekor")]
        public int Quantity { get; set; }

        [StringLength(200, ErrorMessage = "Keterangan maksimal 200 karakter")]
        public string? Description { get; set; }
    }

    public class PermitApprovalHistory
    {
        public int Id { get; set; }

        public int PermitApplicationId { get; set; }
        public virtual LivestockPermitApplication PermitApplication { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        public PermitStatus FromStatus { get; set; }

        public PermitStatus ToStatus { get; set; }

        [Required(ErrorMessage = "Aksi harus diisi")]
        [StringLength(50, ErrorMessage = "Aksi maksimal 50 karakter")]
        public string Action { get; set; } = string.Empty; // "Submit", "Approve", "Reject", "Review"

        [StringLength(1000, ErrorMessage = "Komentar maksimal 1000 karakter")]
        public string? Comments { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.Now;
    }

    public class PermitDocument
    {
        public int Id { get; set; }

        public int PermitApplicationId { get; set; }
        public virtual LivestockPermitApplication PermitApplication { get; set; }

        [Required(ErrorMessage = "Nama dokumen harus diisi")]
        [StringLength(200, ErrorMessage = "Nama dokumen maksimal 200 karakter")]
        public string DocumentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Path file harus diisi")]
        [StringLength(500, ErrorMessage = "Path file maksimal 500 karakter")]
        public string FilePath { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tipe dokumen harus diisi")]
        [StringLength(50, ErrorMessage = "Tipe dokumen maksimal 50 karakter")]
        public string DocumentType { get; set; } = string.Empty; // "Application", "Supporting", "Generated"

        public long FileSize { get; set; }

        [StringLength(10, ErrorMessage = "Ekstensi file maksimal 10 karakter")]
        public string FileExtension { get; set; } = string.Empty;

        public DateTime UploadDate { get; set; } = DateTime.Now;

        public int UploadedByUserId { get; set; }
        public virtual User UploadedByUser { get; set; }
    }

    public enum PermitStatus
    {
        [Display(Name = "Draft")]
        Draft = 1,

        [Display(Name = "Diajukan")]
        Submitted = 2,

        [Display(Name = "Sedang Diverifikasi")]
        UnderReview = 3,

        [Display(Name = "Disetujui Verifikator")]
        VerifikatorApproved = 4,

        [Display(Name = "Ditolak Verifikator")]
        VerifikatorRejected = 5,

        [Display(Name = "Menunggu Kepala Dinas")]
        PendingKepalaDinas = 6,

        [Display(Name = "Ditolak Kepala Dinas")]
        KepalaDinasRejected = 7,

        [Display(Name = "Disetujui")]
        FinalApproved = 8,

        [Display(Name = "Ditolak")]
        FinalRejected = 9
    }

    // Helper class untuk status display
    public static class PermitStatusHelper
    {
        public static string GetStatusText(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => "Draft",
                PermitStatus.Submitted => "Diajukan",
                PermitStatus.UnderReview => "Sedang Diverifikasi",
                PermitStatus.VerifikatorApproved => "Disetujui Verifikator",
                PermitStatus.VerifikatorRejected => "Ditolak Verifikator",
                PermitStatus.PendingKepalaDinas => "Menunggu Kepala Dinas",
                PermitStatus.KepalaDinasRejected => "Ditolak Kepala Dinas",
                PermitStatus.FinalApproved => "Disetujui",
                PermitStatus.FinalRejected => "Ditolak",
                _ => "Tidak Diketahui"
            };
        }

        public static string GetStatusClass(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => "secondary",
                PermitStatus.Submitted => "info",
                PermitStatus.UnderReview => "warning",
                PermitStatus.VerifikatorApproved => "primary",
                PermitStatus.VerifikatorRejected => "danger",
                PermitStatus.PendingKepalaDinas => "warning",
                PermitStatus.KepalaDinasRejected => "danger",
                PermitStatus.FinalApproved => "success",
                PermitStatus.FinalRejected => "danger",
                _ => "secondary"
            };
        }
    }
}