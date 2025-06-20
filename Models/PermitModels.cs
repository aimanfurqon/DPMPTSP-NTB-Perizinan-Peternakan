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

        // Admin approval (Level 1)
        public int? AdminId { get; set; }
        public virtual User? Admin { get; set; }
        public DateTime? AdminApprovalDate { get; set; }

        // Verifikator approval (Level 2)
        public int? VerifikatorId { get; set; }
        public virtual User? Verifikator { get; set; }
        public DateTime? VerificationDate { get; set; }

        // Kepala Dinas approval (Level 3)
        public int? KepalaDinasId { get; set; }
        public virtual User? KepalaDinas { get; set; }
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
        public string Action { get; set; } = string.Empty;

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
        public string DocumentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [StringLength(10, ErrorMessage = "Ekstensi file maksimal 10 karakter")]
        public string FileExtension { get; set; } = string.Empty;

        public DateTime UploadDate { get; set; } = DateTime.Now;

        public int UploadedByUserId { get; set; }
        public virtual User UploadedByUser { get; set; }
    }

    // Updated Status Enum with new flow
    public enum PermitStatus
    {
        [Display(Name = "Draft")]
        Draft = 1,

        [Display(Name = "Diajukan")]
        Submitted = 2,

        [Display(Name = "Sedang Dicek Admin")]
        UnderAdminReview = 3,

        [Display(Name = "Disetujui Admin")]
        AdminApproved = 4,

        [Display(Name = "Ditolak Admin")]
        AdminRejected = 5,

        [Display(Name = "Sedang Diverifikasi")]
        UnderVerifikatorReview = 6,

        [Display(Name = "Disetujui Verifikator")]
        VerifikatorApproved = 7,

        [Display(Name = "Ditolak Verifikator")]
        VerifikatorRejected = 8,

        [Display(Name = "Menunggu Kepala Dinas")]
        PendingKepalaDinas = 9,

        [Display(Name = "Ditolak Kepala Dinas")]
        KepalaDinasRejected = 10,

        [Display(Name = "Disetujui")]
        FinalApproved = 11,

        [Display(Name = "Ditolak")]
        FinalRejected = 12
    }

    // Updated Helper class
    public static class PermitStatusHelper
    {
        public static string GetStatusText(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => "Draft",
                PermitStatus.Submitted => "Diajukan",
                PermitStatus.UnderAdminReview => "Sedang Dicek Admin",
                PermitStatus.AdminApproved => "Disetujui Admin",
                PermitStatus.AdminRejected => "Ditolak Admin",
                PermitStatus.UnderVerifikatorReview => "Sedang Diverifikasi",
                PermitStatus.VerifikatorApproved => "Disetujui Verifikator",
                PermitStatus.VerifikatorRejected => "Ditolak Verifikator",
                PermitStatus.PendingKepalaDinas => "Menunggu Kepala Dinas",
                PermitStatus.KepalaDinasRejected => "Ditolak Kepala Dinas",
                PermitStatus.FinalApproved => "Disetujui",
                PermitStatus.FinalRejected => "Ditolak",
                _ => "Tidak Diketahui"
            };
        }

        //public static string GetStatusClass(PermitStatus status)
        //{
        //    return status switch
        //    {
        //        PermitStatus.Draft => "secondary",
        //        PermitStatus.Submitted => "info",
        //        PermitStatus.UnderAdminReview => "warning",
        //        PermitStatus.AdminApproved => "primary",
        //        PermitStatus.AdminRejected => "danger",
        //        PermitStatus.UnderVerifikatorReview => "warning",
        //        PermitStatus.VerifikatorApproved => "primary",
        //        PermitStatus.VerifikatorRejected => "danger",
        //        PermitStatus.PendingKepalaDinas => "warning",
        //        PermitStatus.KepalaDinasRejected => "danger",
        //        PermitStatus.FinalApproved => "success",
        //        PermitStatus.FinalRejected => "danger",
        //        _ => "secondary"
        //    };
        //}

        public static string GetStatusClass(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => "status-draft",
                PermitStatus.Submitted => "status-submitted",
                PermitStatus.UnderAdminReview => "status-under-review",
                PermitStatus.AdminApproved => "status-admin-approved",
                PermitStatus.AdminRejected => "status-rejected",
                PermitStatus.UnderVerifikatorReview => "status-under-review",
                PermitStatus.VerifikatorApproved => "status-verifikator-approved",
                PermitStatus.VerifikatorRejected => "status-rejected",
                PermitStatus.PendingKepalaDinas => "status-pending",
                PermitStatus.FinalApproved => "status-approved",
                PermitStatus.KepalaDinasRejected => "status-rejected",
                _ => "status-unknown"
            };
        }

        public static string GetStatusIcon(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => "fas fa-edit",
                PermitStatus.Submitted => "fas fa-paper-plane",
                PermitStatus.UnderAdminReview => "fas fa-clock",
                PermitStatus.AdminApproved => "fas fa-check",
                PermitStatus.AdminRejected => "fas fa-times",
                PermitStatus.UnderVerifikatorReview => "fas fa-search",
                PermitStatus.VerifikatorApproved => "fas fa-check-circle",
                PermitStatus.VerifikatorRejected => "fas fa-times-circle",
                PermitStatus.PendingKepalaDinas => "fas fa-user-tie",
                PermitStatus.KepalaDinasRejected => "fas fa-ban",
                PermitStatus.FinalApproved => "fas fa-stamp",
                PermitStatus.FinalRejected => "fas fa-times-circle",
                _ => "fas fa-question-circle"
            };
        }

        public static string GetNextStepText(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => "Siap untuk diajukan",
                PermitStatus.Submitted => "Menunggu review dari Admin",
                PermitStatus.UnderAdminReview => "Sedang dalam review Admin",
                PermitStatus.AdminApproved => "Menunggu verifikasi dari Verifikator",
                PermitStatus.AdminRejected => "Permohonan ditolak oleh Admin",
                PermitStatus.UnderVerifikatorReview => "Sedang dalam verifikasi",
                PermitStatus.VerifikatorApproved => "Menunggu persetujuan dari Kepala Dinas",
                PermitStatus.VerifikatorRejected => "Permohonan ditolak oleh Verifikator",
                PermitStatus.PendingKepalaDinas => "Menunggu keputusan Kepala Dinas",
                PermitStatus.KepalaDinasRejected => "Permohonan ditolak oleh Kepala Dinas",
                PermitStatus.FinalApproved => "Izin telah diterbitkan",
                PermitStatus.FinalRejected => "Permohonan ditolak final",
                _ => "Status tidak diketahui"
            };
        }

        public static bool CanDownload(PermitStatus status, string userRole)
        {
            if (userRole == "User")
            {
                return status == PermitStatus.FinalApproved;
            }

            // Admin, Verifikator, KepalaDinas bisa download jika sudah ada dokumen
            return status >= PermitStatus.AdminApproved;
        }

        public static bool CanView(PermitStatus status, string userRole)
        {
            return userRole switch
            {
                "User" => true, // User bisa lihat semua permohonannya
                "Admin" => status >= PermitStatus.Submitted,
                "Verifikator" => status >= PermitStatus.AdminApproved,
                "KepalaDinas" => status >= PermitStatus.VerifikatorApproved,
                _ => false
            };
        }

        public static bool CanApprove(PermitStatus status, string userRole)
        {
            return userRole switch
            {
                "Admin" => status == PermitStatus.Submitted || status == PermitStatus.UnderAdminReview,
                "Verifikator" => status == PermitStatus.AdminApproved || status == PermitStatus.UnderVerifikatorReview,
                "KepalaDinas" => status == PermitStatus.VerifikatorApproved || status == PermitStatus.PendingKepalaDinas,
                _ => false
            };
        }

        public static PermitStatus GetNextApprovalStatus(PermitStatus currentStatus)
        {
            return currentStatus switch
            {
                PermitStatus.Submitted => PermitStatus.AdminApproved,
                PermitStatus.UnderAdminReview => PermitStatus.AdminApproved,
                PermitStatus.AdminApproved => PermitStatus.VerifikatorApproved,
                PermitStatus.UnderVerifikatorReview => PermitStatus.VerifikatorApproved,
                PermitStatus.VerifikatorApproved => PermitStatus.FinalApproved,
                PermitStatus.PendingKepalaDinas => PermitStatus.FinalApproved,
                _ => currentStatus
            };
        }

        public static string GetApproverRoleForStatus(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Submitted => "Admin",
                PermitStatus.UnderAdminReview => "Admin",
                PermitStatus.AdminApproved => "Verifikator",
                PermitStatus.UnderVerifikatorReview => "Verifikator",
                PermitStatus.VerifikatorApproved => "KepalaDinas",
                PermitStatus.PendingKepalaDinas => "KepalaDinas",
                _ => ""
            };
        }

        public static int GetProgressPercentage(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => 0,
                PermitStatus.Submitted => 25,
                PermitStatus.UnderAdminReview => 25,
                PermitStatus.AdminApproved => 50,
                PermitStatus.UnderVerifikatorReview => 50,
                PermitStatus.VerifikatorApproved => 75,
                PermitStatus.PendingKepalaDinas => 75,
                PermitStatus.FinalApproved => 100,
                PermitStatus.AdminRejected => 0,
                PermitStatus.VerifikatorRejected => 0,
                PermitStatus.KepalaDinasRejected => 0,
                PermitStatus.FinalRejected => 0,
                _ => 0
            };
        }
    }
}