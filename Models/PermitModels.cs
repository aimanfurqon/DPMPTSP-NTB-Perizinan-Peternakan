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

        [StringLength(200, ErrorMessage = "Nama perusahaan maksimal 200 karakter")]
        public string CompanyName { get; set; } = string.Empty;

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

        [Display(Name = "Provinsi Asal")]
        public int? OriginProvinceId { get; set; }

        [Display(Name = "Kabupaten/Kota Asal")]
        public int? OriginRegencyId { get; set; }

        [Display(Name = "Provinsi Tujuan")]
        public int? DestinationProvinceId { get; set; }

        [Display(Name = "Kabupaten/Kota Tujuan")]
        public int? DestinationRegencyId { get; set; }
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

        [Display(Name = "Tanggal Pengajuan Dokumen")]
        [DataType(DataType.Date)]
        public DateTime? DocumentDate { get; set; }

        [Display(Name = "Nomor Dokumen")]
        [StringLength(50, ErrorMessage = "Nomor dokumen maksimal 50 karakter")]
        public string? DocumentNumber { get; set; }

        [Display(Name = "Keterangan Dokumen")]
        [StringLength(500, ErrorMessage = "Keterangan dokumen maksimal 500 karakter")]
        public string? DocumentDescription { get; set; }

        public bool HasDocumentDetails => DocumentDate.HasValue || !string.IsNullOrEmpty(DocumentNumber);

        public string FormattedDocumentDetails
        {
            get
            {
                var details = new List<string>();

                if (DocumentDate.HasValue)
                    details.Add($"Tanggal: {DocumentDate.Value:dd/MM/yyyy}");

                if (!string.IsNullOrEmpty(DocumentNumber))
                    details.Add($"No: {DocumentNumber}");

                return string.Join(" | ", details);
            }
        }
    }

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

    public static class PermitStatusHelper
    {
        public static string GetStatusClass(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => "secondary",
                PermitStatus.Submitted => "warning",
                PermitStatus.UnderAdminReview => "info",
                PermitStatus.AdminApproved => "primary",
                PermitStatus.AdminRejected => "danger",
                PermitStatus.UnderVerifikatorReview => "info",
                PermitStatus.VerifikatorApproved => "primary",
                PermitStatus.VerifikatorRejected => "danger",
                PermitStatus.PendingKepalaDinas => "info",
                PermitStatus.KepalaDinasRejected => "danger",
                PermitStatus.FinalApproved => "success",
                _ => "secondary"
            };
        }

        public static string GetStatusText(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => "Draft",
                PermitStatus.Submitted => "Diajukan",
                PermitStatus.UnderAdminReview => "Review Admin",
                PermitStatus.AdminApproved => "Disetujui Admin",
                PermitStatus.AdminRejected => "Ditolak Admin",
                PermitStatus.UnderVerifikatorReview => "Review Verifikator",
                PermitStatus.VerifikatorApproved => "Disetujui Verifikator",
                PermitStatus.VerifikatorRejected => "Ditolak Verifikator",
                PermitStatus.PendingKepalaDinas => "Menunggu Kepala Dinas",
                PermitStatus.KepalaDinasRejected => "Ditolak Kepala Dinas",
                PermitStatus.FinalApproved => "Disetujui Final",
                _ => "Tidak Diketahui"
            };
        }

        public static int GetProgressPercentage(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => 0,
                PermitStatus.Submitted => 25,
                PermitStatus.UnderAdminReview => 30,
                PermitStatus.AdminApproved => 50,
                PermitStatus.AdminRejected => 0,
                PermitStatus.UnderVerifikatorReview => 60,
                PermitStatus.VerifikatorApproved => 75,
                PermitStatus.VerifikatorRejected => 0,
                PermitStatus.PendingKepalaDinas => 85,
                PermitStatus.KepalaDinasRejected => 0,
                PermitStatus.FinalApproved => 100,
                _ => 0
            };
        }

        public static string GetProgressText(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => "Belum Diajukan",
                PermitStatus.Submitted => "Diajukan",
                PermitStatus.UnderAdminReview => "Review Admin",
                PermitStatus.AdminApproved => "Disetujui Admin",
                PermitStatus.AdminRejected => "Ditolak Admin",
                PermitStatus.UnderVerifikatorReview => "Review Verifikator",
                PermitStatus.VerifikatorApproved => "Disetujui Verifikator",
                PermitStatus.VerifikatorRejected => "Ditolak Verifikator",
                PermitStatus.PendingKepalaDinas => "Menunggu Kepala Dinas",
                PermitStatus.KepalaDinasRejected => "Ditolak Kepala Dinas",
                PermitStatus.FinalApproved => "Selesai",
                _ => "Tidak Diketahui"
            };
        }

        public static string GetStatusIcon(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => "fas fa-edit",
                PermitStatus.Submitted => "fas fa-paper-plane",
                PermitStatus.UnderAdminReview => "fas fa-search",
                PermitStatus.AdminApproved => "fas fa-user-check",
                PermitStatus.AdminRejected => "fas fa-user-times",
                PermitStatus.UnderVerifikatorReview => "fas fa-clipboard-check",
                PermitStatus.VerifikatorApproved => "fas fa-clipboard-check",
                PermitStatus.VerifikatorRejected => "fas fa-clipboard-list",
                PermitStatus.PendingKepalaDinas => "fas fa-user-tie",
                PermitStatus.KepalaDinasRejected => "fas fa-user-slash",
                PermitStatus.FinalApproved => "fas fa-award",
                _ => "fas fa-question"
            };
        }

        public static List<ProgressStep> GetProgressSteps(PermitStatus currentStatus)
        {
            var steps = new List<ProgressStep>
            {
                new ProgressStep
                {
                    Title = "Diajukan",
                    Icon = "fas fa-paper-plane",
                    IsCompleted = currentStatus >= PermitStatus.Submitted,
                    IsCurrent = currentStatus == PermitStatus.Submitted
                },
                new ProgressStep
                {
                    Title = "Review Admin",
                    Icon = "fas fa-user-check",
                    IsCompleted = currentStatus >= PermitStatus.AdminApproved,
                    IsCurrent = currentStatus == PermitStatus.UnderAdminReview || currentStatus == PermitStatus.AdminApproved
                },
                new ProgressStep
                {
                    Title = "Verifikasi",
                    Icon = "fas fa-clipboard-check",
                    IsCompleted = currentStatus >= PermitStatus.VerifikatorApproved,
                    IsCurrent = currentStatus == PermitStatus.UnderVerifikatorReview || currentStatus == PermitStatus.VerifikatorApproved
                },
                new ProgressStep
                {
                    Title = "Persetujuan Final",
                    Icon = "fas fa-award",
                    IsCompleted = currentStatus == PermitStatus.FinalApproved,
                    IsCurrent = currentStatus == PermitStatus.PendingKepalaDinas || currentStatus == PermitStatus.FinalApproved
                }
            };

            if (IsRejectedStatus(currentStatus))
            {
                var rejectedStep = steps.FirstOrDefault(s => s.IsCurrent);
                if (rejectedStep != null)
                {
                    rejectedStep.IsRejected = true;
                    rejectedStep.IsCurrent = true;
                    rejectedStep.IsCompleted = false;
                }
            }

            return steps;
        }

        public static bool IsRejectedStatus(PermitStatus status)
        {
            return status == PermitStatus.AdminRejected ||
                   status == PermitStatus.VerifikatorRejected ||
                   status == PermitStatus.KepalaDinasRejected;
        }

        public static int GetApprovalLevel(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => 0,
                PermitStatus.Submitted => 1,
                PermitStatus.UnderAdminReview => 1,
                PermitStatus.AdminApproved => 2,
                PermitStatus.AdminRejected => 1,
                PermitStatus.UnderVerifikatorReview => 2,
                PermitStatus.VerifikatorApproved => 3,
                PermitStatus.VerifikatorRejected => 2,
                PermitStatus.PendingKepalaDinas => 3,
                PermitStatus.KepalaDinasRejected => 3,
                PermitStatus.FinalApproved => 4,
                _ => 0
            };
        }

        public static int GetEstimatedProcessingDays(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Submitted => 3,
                PermitStatus.UnderAdminReview => 2,
                PermitStatus.AdminApproved => 5,
                PermitStatus.UnderVerifikatorReview => 3,
                PermitStatus.VerifikatorApproved => 7,
                PermitStatus.PendingKepalaDinas => 5,
                PermitStatus.FinalApproved => 0,
                _ => 0
            };
        }

        public static string GetStatusDescription(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => "Permohonan sedang disusun dan belum diajukan",
                PermitStatus.Submitted => "Permohonan telah diajukan dan menunggu review dari admin",
                PermitStatus.UnderAdminReview => "Permohonan sedang dalam review oleh admin",
                PermitStatus.AdminApproved => "Permohonan telah disetujui admin dan akan diverifikasi",
                PermitStatus.AdminRejected => "Permohonan ditolak oleh admin",
                PermitStatus.UnderVerifikatorReview => "Permohonan sedang dalam proses verifikasi",
                PermitStatus.VerifikatorApproved => "Permohonan telah diverifikasi dan menunggu persetujuan final",
                PermitStatus.VerifikatorRejected => "Permohonan ditolak oleh verifikator",
                PermitStatus.PendingKepalaDinas => "Permohonan menunggu persetujuan dari Kepala Dinas",
                PermitStatus.KepalaDinasRejected => "Permohonan ditolak oleh Kepala Dinas",
                PermitStatus.FinalApproved => "Permohonan telah disetujui secara final dan dapat diunduh",
                _ => "Status tidak diketahui"
            };
        }

        public static string GetProgressBarColor(PermitStatus status)
        {
            if (IsRejectedStatus(status))
            {
                return "bg-danger";
            }

            return status switch
            {
                PermitStatus.Draft => "bg-secondary",
                PermitStatus.Submitted => "bg-warning",
                PermitStatus.UnderAdminReview => "bg-info",
                PermitStatus.AdminApproved => "bg-primary",
                PermitStatus.UnderVerifikatorReview => "bg-info",
                PermitStatus.VerifikatorApproved => "bg-primary",
                PermitStatus.PendingKepalaDinas => "bg-info",
                PermitStatus.FinalApproved => "bg-success",
                _ => "bg-secondary"
            };
        }
    }

    public class ProgressStep
    {
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "";
        public bool IsCompleted { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsRejected { get; set; }
    }
}