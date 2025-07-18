using System.ComponentModel.DataAnnotations;
using PerizinanPeternakan.Models;
using Microsoft.AspNetCore.Http;

namespace PerizinanPeternakan.ViewModels
{
    public class PermitApplicationViewModel
    {


        // Tambahkan properties baru untuk dropdown
        [Display(Name = "Provinsi Asal")]
        public int? OriginProvinceId { get; set; }

        [Display(Name = "Kabupaten/Kota Asal")]
        public int? OriginRegencyId { get; set; }

        [Display(Name = "Provinsi Tujuan")]
        public int? DestinationProvinceId { get; set; }

        [Display(Name = "Kabupaten/Kota Tujuan")]
        public int? DestinationRegencyId { get; set; }

        // Ubah OriginLocation dan DestinationLocation menjadi tidak required
        [StringLength(100, ErrorMessage = "Asal ternak maksimal 100 karakter")]
        [Display(Name = "Asal Ternak")]
        public string OriginLocation { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Tujuan pengiriman maksimal 100 karakter")]
        [Display(Name = "Tujuan Pengiriman")]
        public string DestinationLocation { get; set; } = string.Empty;

        // Di atas adalah tambahan

        [Required(ErrorMessage = "Nama perusahaan harus diisi")]
        [StringLength(200, ErrorMessage = "Nama perusahaan maksimal 200 karakter")]
        [Display(Name = "Nama Perusahaan")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Alamat perusahaan harus diisi")]
        [StringLength(500, ErrorMessage = "Alamat perusahaan maksimal 500 karakter")]
        [Display(Name = "Alamat Perusahaan")]
        public string CompanyAddress { get; set; } = string.Empty;

        //[Required(ErrorMessage = "Asal ternak harus diisi")]
        //[StringLength(100, ErrorMessage = "Asal ternak maksimal 100 karakter")]
        //[Display(Name = "Asal Ternak")]
        //public string OriginLocation { get; set; } = string.Empty;

        //[Required(ErrorMessage = "Tujuan pengiriman harus diisi")]
        //[StringLength(100, ErrorMessage = "Tujuan pengiriman maksimal 100 karakter")]
        //[Display(Name = "Tujuan Pengiriman")]
        //public string DestinationLocation { get; set; } = string.Empty;

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

        // Document Upload Properties
        [Display(Name = "Surat Permohonan")]
        public IFormFile? SuratPermohonan { get; set; }

        [Display(Name = "Surat Rekomendasi dari Dinas Peternakan Provinsi NTB")]
        public IFormFile? RekomendasiDinasProv { get; set; }

        [Display(Name = "Rekomendasi Pemasukan Ternak dari Daerah Tujuan")]
        public IFormFile? RekomendasiDaerahTujuan { get; set; }

        [Display(Name = "SKKH dari Kabupaten Asal")]
        public IFormFile? SKKHKabupatenAsal { get; set; }

        [Display(Name = "SKKH dari Dinas Peternakan Provinsi NTB")]
        public IFormFile? SKKHDinasProvinsi { get; set; }

        [Display(Name = "Surat Keterangan Jalan Ternak/Rekomendasi Asal")]
        public IFormFile? SuratJalanTernak { get; set; }

        [Display(Name = "Hasil Pemeriksaan Fisik (Holding Ground)")]
        public IFormFile? HasilPemeriksaanFisik { get; set; }
    }

    public class AdminHistoryViewModel
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; }
        public string CompanyName { get; set; }
        public string ApplicantName { get; set; }
        public PermitStatus Status { get; set; }
        public DateTime SubmissionDate { get; set; }
        public DateTime? AdminApprovalDate { get; set; }
        public string AdminComments { get; set; }
        public string AdminAction { get; set; }
        public int DocumentCount { get; set; }
        public bool CanView { get; set; }

        // Helper properties
        public string StatusText => PermitStatusHelper.GetStatusText(Status);
        public string StatusClass => PermitStatusHelper.GetStatusClass(Status);
        public string FormattedAdminApprovalDate => AdminApprovalDate?.ToString("dd MMM yyyy HH:mm") ?? "-";
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

        // Document Information untuk ditampilkan di approval page
        [Display(Name = "Dokumen Pendukung")]
        public List<DocumentViewModel> Documents { get; set; } = new List<DocumentViewModel>();
    }

    public class PermitListViewModel
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public PermitStatus Status { get; set; }
        public DateTime SubmissionDate { get; set; }

        // Added properties for new approval flow
        public DateTime? AdminApprovalDate { get; set; }
        public DateTime? VerificationDate { get; set; }
        public DateTime? FinalApprovalDate { get; set; }

        public string OriginLocation { get; set; } = string.Empty;
        public string DestinationLocation { get; set; } = string.Empty;
        public bool CanDownload { get; set; }
        public bool CanView { get; set; }
        public bool CanApprove { get; set; }

        // Additional properties for better display
        public string? GeneratedDocumentPath { get; set; }
        public int CurrentApprovalLevel { get; set; }
        public string? AdminName { get; set; }
        public string? VerifikatorName { get; set; }
        public string? KepalaDinasName { get; set; }

        // Document count for quick overview
        public int DocumentCount { get; set; }
        public bool HasAllRequiredDocuments { get; set; }
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

        // Added approval dates for new flow
        public DateTime? AdminApprovalDate { get; set; }
        public DateTime? VerificationDate { get; set; }
        public DateTime? FinalApprovalDate { get; set; }

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

        // Added approver information
        public string? AdminName { get; set; }
        public string? VerifikatorName { get; set; }
        public string? KepalaDinasName { get; set; }
        public int CurrentApprovalLevel { get; set; }

        // Document Information
        public List<DocumentViewModel> Documents { get; set; } = new List<DocumentViewModel>();
    }

    public class DocumentViewModel
    {
        public int Id { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public string FormattedFileSize => FormatFileSize(FileSize);
        public string DocumentDisplayName => GetDocumentDisplayName(DocumentType);

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string GetDocumentDisplayName(string documentType)
        {
            return documentType switch
            {
                "SURAT_PERMOHONAN" => "Surat Permohonan",
                "REKOMENDASI_DINAS_PROV" => "Rekomendasi Dinas Peternakan Provinsi NTB",
                "REKOMENDASI_DAERAH_TUJUAN" => "Rekomendasi Pemasukan Ternak dari Daerah Tujuan",
                "SKKH_KABUPATEN_ASAL" => "SKKH dari Kabupaten Asal",
                "SKKH_DINAS_PROVINSI" => "SKKH dari Dinas Peternakan Provinsi NTB",
                "SURAT_JALAN_TERNAK" => "Surat Keterangan Jalan Ternak/Rekomendasi Asal",
                "HASIL_PEMERIKSAAN_FISIK" => "Hasil Pemeriksaan Fisik (Holding Ground)",
                _ => documentType
            };
        }
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

        // Additional stats for new flow
        public int PendingAdminReview { get; set; }
        public int PendingVerifikatorReview { get; set; }
        public int AdminApprovedThisMonth { get; set; }
        public int VerifikatorApprovedThisMonth { get; set; }

        public List<PermitListViewModel> RecentApplications { get; set; } = new List<PermitListViewModel>();
        public List<PermitListViewModel> MyPendingApprovals { get; set; } = new List<PermitListViewModel>();
    }

    public class PermitDocumentViewModel
    {
        public int PermitId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public PermitStatus Status { get; set; }
        public string DocumentContent { get; set; } = string.Empty;
        public bool CanApprove { get; set; }
        public string UserRole { get; set; } = string.Empty;
    }
}