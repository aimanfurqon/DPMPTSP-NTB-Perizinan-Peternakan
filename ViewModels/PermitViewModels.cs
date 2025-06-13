using System.ComponentModel.DataAnnotations;
using PerizinanPeternakan.Models;

namespace PerizinanPeternakan.ViewModels
{
    public class PermitApplicationViewModel
    {
        [Required(ErrorMessage = "Nama perusahaan harus diisi")]
        [StringLength(200, ErrorMessage = "Nama perusahaan maksimal 200 karakter")]
        [Display(Name = "Nama Perusahaan")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Alamat perusahaan harus diisi")]
        [StringLength(500, ErrorMessage = "Alamat perusahaan maksimal 500 karakter")]
        [Display(Name = "Alamat Perusahaan")]
        public string CompanyAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Asal ternak harus diisi")]
        [StringLength(100, ErrorMessage = "Asal ternak maksimal 100 karakter")]
        [Display(Name = "Asal Ternak")]
        public string OriginLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tujuan pengiriman harus diisi")]
        [StringLength(100, ErrorMessage = "Tujuan pengiriman maksimal 100 karakter")]
        [Display(Name = "Tujuan Pengiriman")]
        public string DestinationLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pelabuhan asal harus diisi")]
        [StringLength(100, ErrorMessage = "Pelabuhan asal maksimal 100 karakter")]
        [Display(Name = "Pelabuhan Asal")]
        public string DeparturePort { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pelabuhan bongkar harus diisi")]
        [StringLength(100, ErrorMessage = "Pelabuhan bongkar maksimal 100 karakter")]
        [Display(Name = "Pelabuhan Bongkar")]
        public string ArrivalPort { get; set; } = string.Empty;

        [Display(Name = "Detail Ternak")]
        public List<LivestockDetailViewModel> LivestockDetails { get; set; } = new List<LivestockDetailViewModel>();
    }

    public class LivestockDetailViewModel
    {
        [Required(ErrorMessage = "Jenis ternak harus diisi")]
        [StringLength(50, ErrorMessage = "Jenis ternak maksimal 50 karakter")]
        [Display(Name = "Jenis Ternak")]
        public string LivestockType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jumlah ternak harus diisi")]
        [Range(1, 10000, ErrorMessage = "Jumlah ternak harus antara 1-10000 ekor")]
        [Display(Name = "Jumlah (ekor)")]
        public int Quantity { get; set; }

        [StringLength(200, ErrorMessage = "Keterangan maksimal 200 karakter")]
        [Display(Name = "Keterangan")]
        public string? Description { get; set; }
    }

    public class PermitApprovalViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Nomor Permohonan")]
        public string ApplicationNumber { get; set; } = string.Empty;

        [Display(Name = "Nama Perusahaan")]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "Pemohon")]
        public string ApplicantName { get; set; } = string.Empty;

        [Display(Name = "Status Saat Ini")]
        public PermitStatus CurrentStatus { get; set; }

        [Display(Name = "Tanggal Pengajuan")]
        public DateTime SubmissionDate { get; set; }

        [Display(Name = "Asal Ternak")]
        public string OriginLocation { get; set; } = string.Empty;

        [Display(Name = "Tujuan Pengiriman")]
        public string DestinationLocation { get; set; } = string.Empty;

        [Display(Name = "Detail Ternak")]
        public List<LivestockDetailViewModel> LivestockDetails { get; set; } = new List<LivestockDetailViewModel>();

        [Display(Name = "Komentar/Catatan")]
        [StringLength(1000, ErrorMessage = "Komentar maksimal 1000 karakter")]
        public string? Comments { get; set; }

        [Display(Name = "Aksi")]
        public string Action { get; set; } = string.Empty; // "Approve" atau "Reject"
    }

    public class PermitListViewModel
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public PermitStatus Status { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string OriginLocation { get; set; } = string.Empty;
        public string DestinationLocation { get; set; } = string.Empty;
        public bool CanDownload { get; set; }
        public bool CanView { get; set; }
        public bool CanApprove { get; set; }
    }

    public class PermitDetailViewModel
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string ApplicantPhone { get; set; } = string.Empty;
        public PermitStatus Status { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string OriginLocation { get; set; } = string.Empty;
        public string DestinationLocation { get; set; } = string.Empty;
        public string DeparturePort { get; set; } = string.Empty;
        public string ArrivalPort { get; set; } = string.Empty;
        public List<LivestockDetailViewModel> LivestockDetails { get; set; } = new List<LivestockDetailViewModel>();
        public List<ApprovalHistoryViewModel> ApprovalHistory { get; set; } = new List<ApprovalHistoryViewModel>();
        public string? RejectionReason { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public bool CanDownload { get; set; }
        public bool CanApprove { get; set; }
        public string? GeneratedDocumentPath { get; set; }
    }

    public class ApprovalHistoryViewModel
    {
        public string Action { get; set; } = string.Empty;
        public string ActionBy { get; set; } = string.Empty;
        public string ActionByRole { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; }
        public string? Comments { get; set; }
        public PermitStatus FromStatus { get; set; }
        public PermitStatus ToStatus { get; set; }
    }

    public class DashboardStatsViewModel
    {
        public int TotalApplications { get; set; }
        public int PendingVerification { get; set; }
        public int PendingKepalaDinas { get; set; }
        public int ApprovedThisMonth { get; set; }
        public int RejectedThisMonth { get; set; }
        public List<PermitListViewModel> RecentApplications { get; set; } = new List<PermitListViewModel>();
        public List<PermitListViewModel> MyPendingApprovals { get; set; } = new List<PermitListViewModel>();
    }
}