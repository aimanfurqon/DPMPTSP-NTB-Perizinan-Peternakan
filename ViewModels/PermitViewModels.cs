using System.ComponentModel.DataAnnotations;
using PerizinanPeternakan.Models;
using Microsoft.AspNetCore.Http;

namespace PerizinanPeternakan.ViewModels
{
    public class PermitApplicationViewModel
    {
        [Display(Name = "Dokumen Opsional")]
        public IFormFile? DokumenOpsional { get; set; }

        [Display(Name = "Tanggal Dokumen Opsional")]
        [DataType(DataType.Date)]
        public DateTime? DokumenOpsionalTanggal { get; set; }

        [Display(Name = "Nomor Dokumen Opsional")]
        public string? DokumenOpsionalNomor { get; set; }

        [Display(Name = "Nama Dokumen Opsional")]
        public string? DokumenOpsionalNama { get; set; }

        [Display(Name = "Tipe Pemohon")]
        public string ApplicantType { get; set; } = "Company"; 

        [Display(Name = "Provinsi Asal")]
        public int? OriginProvinceId { get; set; }

        [Display(Name = "Kabupaten/Kota Asal")]
        public int? OriginRegencyId { get; set; }

        [Display(Name = "Provinsi Tujuan")]
        public int? DestinationProvinceId { get; set; }

        [Display(Name = "Kabupaten/Kota Tujuan")]
        public int? DestinationRegencyId { get; set; }

        [StringLength(100, ErrorMessage = "Asal ternak maksimal 100 karakter")]
        [Display(Name = "Asal Ternak")]
        public string OriginLocation { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Tujuan pengiriman maksimal 100 karakter")]
        [Display(Name = "Tujuan Pengiriman")]
        public string DestinationLocation { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Nama perusahaan maksimal 200 karakter")]
        [Display(Name = "Nama Perusahaan")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Alamat perusahaan maksimal 500 karakter")]
        [Display(Name = "Alamat Perusahaan")]
        public string CompanyAddress { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Pelabuhan asal maksimal 100 karakter")]
        [Display(Name = "Pelabuhan Asal")]
        public string DeparturePort { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Pelabuhan bongkar maksimal 100 karakter")]
        [Display(Name = "Pelabuhan Bongkar")]
        public string ArrivalPort { get; set; } = string.Empty;

        [Display(Name = "Detail Ternak")]
        public List<LivestockDetailViewModel> LivestockDetails { get; set; } = new List<LivestockDetailViewModel>();

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

        [Display(Name = "Tanggal Pengajuan Surat Permohonan")]
        [DataType(DataType.Date)]
        public DateTime? SuratPermohonanTanggal { get; set; }

        [Display(Name = "Nomor Dokumen Surat Permohonan")]
        [StringLength(50, ErrorMessage = "Nomor dokumen maksimal 50 karakter")]
        public string? SuratPermohonanNomor { get; set; }

        [Display(Name = "Tanggal Pengajuan Rekomendasi Dinas Provinsi")]
        [DataType(DataType.Date)]
        public DateTime? RekomendasiDinasProvTanggal { get; set; }

        [Display(Name = "Nomor Dokumen Rekomendasi Dinas Provinsi")]
        [StringLength(50, ErrorMessage = "Nomor dokumen maksimal 50 karakter")]
        public string? RekomendasiDinasProvNomor { get; set; }

        [Display(Name = "Tanggal Pengajuan Rekomendasi Daerah Tujuan")]
        [DataType(DataType.Date)]
        public DateTime? RekomendasiDaerahTujuanTanggal { get; set; }

        [Display(Name = "Nomor Dokumen Rekomendasi Daerah Tujuan")]
        [StringLength(50, ErrorMessage = "Nomor dokumen maksimal 50 karakter")]
        public string? RekomendasiDaerahTujuanNomor { get; set; }

        [Display(Name = "Nama Jalan / Dusun")]
        [StringLength(200)]
        public string? AddressStreet { get; set; }

        [Display(Name = "RT")]
        [StringLength(5)]
        public string? AddressRT { get; set; }

        [Display(Name = "RW")]
        [StringLength(5)]
        public string? AddressRW { get; set; }

        [Display(Name = "Desa / Kelurahan")]
        [StringLength(100)]
        public string? AddressVillage { get; set; }

        [Display(Name = "Kecamatan")]
        [StringLength(100)]
        public string? AddressSubDistrict { get; set; }

        [Display(Name = "Provinsi Perusahaan")]
        public string? CompanyProvince { get; set; }

        [Display(Name = "Kabupaten/Kota Perusahaan")]
        public string? CompanyRegency { get; set; }

        [Display(Name = "Kode Pos")]
        [StringLength(10)]
        public string? AddressPostalCode { get; set; }

        [Display(Name = "Kecamatan")]
        [StringLength(100)]
        public string? AddressDistrict { get; set; }

        [Display(Name = "Nama Lengkap")]
        [StringLength(200, ErrorMessage = "Nama lengkap maksimal 200 karakter")]
        public string? IndividualName { get; set; }

        [Display(Name = "Provinsi")]
        public string? IndividualProvince { get; set; }

        [Display(Name = "Kabupaten/Kota")]
        public string? IndividualRegency { get; set; }

        [Display(Name = "Alamat Lengkap")]
        [StringLength(500, ErrorMessage = "Alamat lengkap maksimal 500 karakter")]
        public string? IndividualAddress { get; set; }

        public string GetApplicantName()
        {
            return ApplicantType == "Individual"
                ? IndividualName ?? ""
                : CompanyName ?? "";
        }

        public string GetApplicantAddress()
        {
            if (ApplicantType == "Individual")
            {
                if (!string.IsNullOrEmpty(IndividualAddress))
                {
                    var addressParts = new List<string> { IndividualAddress };
                    if (!string.IsNullOrEmpty(IndividualRegency)) addressParts.Add(IndividualRegency);
                    if (!string.IsNullOrEmpty(IndividualProvince)) addressParts.Add(IndividualProvince);
                    return string.Join(", ", addressParts);
                }
                return "";
            }

            return CompanyAddress ?? "";
        }

        public string GetApplicantTypeLabel()
        {
            return ApplicantType == "Individual" ? "Perorangan" : "Perusahaan";
        }

        public bool IsApplicantDataComplete()
        {
            if (ApplicantType == "Individual")
            {
                return !string.IsNullOrWhiteSpace(IndividualName) &&
                       !string.IsNullOrWhiteSpace(IndividualProvince) &&
                       !string.IsNullOrWhiteSpace(IndividualRegency) &&
                       !string.IsNullOrWhiteSpace(IndividualAddress);
            }
            else
            {
                return !string.IsNullOrWhiteSpace(CompanyName) &&
                       !string.IsNullOrWhiteSpace(CompanyProvince) &&
                       !string.IsNullOrWhiteSpace(CompanyRegency) &&
                       !string.IsNullOrWhiteSpace(CompanyAddress);
            }
        }

        public List<string> GetApplicantValidationErrors()
        {
            var errors = new List<string>();

            if (ApplicantType == "Individual")
            {
                if (string.IsNullOrWhiteSpace(IndividualName))
                    errors.Add("Nama lengkap wajib diisi");

                if (string.IsNullOrWhiteSpace(IndividualProvince))
                    errors.Add("Provinsi wajib diisi");

                if (string.IsNullOrWhiteSpace(IndividualRegency))
                    errors.Add("Kabupaten/Kota wajib diisi");

                if (string.IsNullOrWhiteSpace(IndividualAddress))
                    errors.Add("Alamat lengkap wajib diisi");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(CompanyName))
                    errors.Add("Nama perusahaan wajib diisi");

                if (string.IsNullOrWhiteSpace(CompanyProvince))
                    errors.Add("Provinsi perusahaan wajib diisi");

                if (string.IsNullOrWhiteSpace(CompanyRegency))
                    errors.Add("Kabupaten perusahaan wajib diisi");

                if (string.IsNullOrWhiteSpace(CompanyAddress))
                    errors.Add("Alamat perusahaan wajib diisi");
            }

            return errors;
        }
    }

    public class ConditionalRequiredAttribute : ValidationAttribute
    {
        private readonly string _dependentProperty;
        private readonly object _targetValue;

        public ConditionalRequiredAttribute(string dependentProperty, object targetValue)
        {
            _dependentProperty = dependentProperty;
            _targetValue = targetValue;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var dependentProperty = validationContext.ObjectType.GetProperty(_dependentProperty);
            if (dependentProperty == null)
            {
                return new ValidationResult($"Property {_dependentProperty} not found");
            }

            var dependentValue = dependentProperty.GetValue(validationContext.ObjectInstance, null);

            if (Equals(dependentValue, _targetValue))
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return new ValidationResult(ErrorMessage ?? "Field ini wajib diisi");
                }
            }

            return ValidationResult.Success;
        }
    }

    public class AdminHistoryViewModel
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public PermitStatus Status { get; set; }
        public DateTime SubmissionDate { get; set; }
        public DateTime? AdminApprovalDate { get; set; }
        public string AdminComments { get; set; } = string.Empty;
        public string AdminAction { get; set; } = string.Empty;
        public int DocumentCount { get; set; }
        public bool CanView { get; set; }

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
        public string Action { get; set; } = string.Empty;

        [Display(Name = "Dokumen Pendukung")]
        public List<DocumentViewModel> Documents { get; set; } = new List<DocumentViewModel>();

        public bool HasAllRequiredDocumentsWithDetails
        {
            get
            {
                var requiredDocTypes = new[] { "SURAT_PERMOHONAN", "REKOMENDASI_DINAS_PROV", "REKOMENDASI_DAERAH_TUJUAN" };
                var documentsWithDetails = Documents.Where(d => requiredDocTypes.Contains(d.DocumentType));

                return documentsWithDetails.Any() && documentsWithDetails.All(d => d.HasDocumentDetails);
            }
        }

        public int DocumentsWithDetailsCount => Documents.Count(d => d.HasDocumentDetails);

        public string DocumentCompletionStatus => HasAllRequiredDocumentsWithDetails ? "Lengkap" : "Perlu Review";

        [Display(Name = "Nama Perusahaan")]
        public string EditableCompanyName { get; set; }

        [Display(Name = "Alamat Perusahaan")]
        public string EditableCompanyAddress { get; set; }

        [Display(Name = "Lokasi Asal")]
        public string EditableOriginLocation { get; set; }

        [Display(Name = "Lokasi Tujuan")]
        public string EditableDestinationLocation { get; set; }

        [Display(Name = "Pelabuhan Keberangkatan")]
        public string EditableDeparturePort { get; set; }

        [Display(Name = "Pelabuhan Tiba")]
        public string EditableArrivalPort { get; set; }

        [Display(Name = "Provinsi Asal")]
        public string EditableOriginProvinceId { get; set; }

        [Display(Name = "Kabupaten/Kota Asal")]
        public string EditableOriginRegencyId { get; set; }

        [Display(Name = "Provinsi Tujuan")]
        public string EditableDestinationProvinceId { get; set; }

        [Display(Name = "Kabupaten/Kota Tujuan")]
        public string EditableDestinationRegencyId { get; set; }

        public List<EditableLivestockDetailViewModel> EditableLivestockDetails { get; set; } = new();

        public bool IsEditingData { get; set; }
        public List<string> ChangedFields { get; set; } = new();
    }

    public class PermitListViewModel
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public PermitStatus Status { get; set; }
        public DateTime SubmissionDate { get; set; }
        public DateTime? AdminApprovalDate { get; set; }
        public DateTime? VerificationDate { get; set; }
        public DateTime? FinalApprovalDate { get; set; }
        public string OriginLocation { get; set; } = string.Empty;
        public string DestinationLocation { get; set; } = string.Empty;
        public bool CanDownload { get; set; }
        public bool CanView { get; set; }
        public bool CanApprove { get; set; }
        public string? GeneratedDocumentPath { get; set; }
        public int CurrentApprovalLevel { get; set; }
        public string? AdminName { get; set; }
        public string? VerifikatorName { get; set; }
        public string? KepalaDinasName { get; set; }
        public int DocumentCount { get; set; }
        public bool HasAllRequiredDocuments { get; set; }

        // NEW: Document details indicators
        public bool HasCompleteDocumentDetails { get; set; }
        public int DocumentsWithDetailsCount { get; set; }
        public string DocumentStatusIndicator => HasCompleteDocumentDetails ? "✅ Lengkap" : "⚠️ Perlu Review";
        public string DocumentDetailsSummary => $"{DocumentsWithDetailsCount}/{DocumentCount} dengan detail";
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
        public string? AdminName { get; set; }
        public string? VerifikatorName { get; set; }
        public string? KepalaDinasName { get; set; }
        public int CurrentApprovalLevel { get; set; }
        public List<DocumentViewModel> Documents { get; set; } = new List<DocumentViewModel>();

        // NEW: Helper properties for document analysis
        public bool HasAllRequiredDocumentsWithDetails
        {
            get
            {
                var requiredDocTypes = new[] { "SURAT_PERMOHONAN", "REKOMENDASI_DINAS_PROV", "REKOMENDASI_DAERAH_TUJUAN" };
                var documentsWithDetails = Documents.Where(d => requiredDocTypes.Contains(d.DocumentType));

                return documentsWithDetails.Any() && documentsWithDetails.All(d => d.HasDocumentDetails);
            }
        }

        public string DocumentsSummary
        {
            get
            {
                var totalDocs = Documents.Count;
                var docsWithDetails = Documents.Count(d => d.HasDocumentDetails);
                return $"{totalDocs} dokumen ({docsWithDetails} dengan detail lengkap)";
            }
        }

        public string DocumentCompletionPercentage
        {
            get
            {
                if (Documents.Count == 0) return "0%";
                var percentage = (Documents.Count(d => d.HasDocumentDetails) * 100) / Documents.Count;
                return $"{percentage}%";
            }
        }
    }

    public class DocumentViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Nama Dokumen")]
        public string DocumentName { get; set; } = string.Empty;

        [Display(Name = "Tipe Dokumen")]
        public string DocumentType { get; set; } = string.Empty;

        [Display(Name = "Path File")]
        public string FilePath { get; set; } = string.Empty;

        [Display(Name = "Ukuran File")]
        public long FileSize { get; set; }

        [Display(Name = "Ekstensi File")]
        public string FileExtension { get; set; } = string.Empty;

        [Display(Name = "Tanggal Upload")]
        public DateTime UploadDate { get; set; }

        [Display(Name = "Diupload Oleh")]
        public string UploadedBy { get; set; } = string.Empty;

        // =================================================================
        // NEW PROPERTIES FOR DOCUMENT DETAILS
        // =================================================================

        [Display(Name = "Tanggal Dokumen")]
        [DataType(DataType.Date)]
        public DateTime? DocumentDate { get; set; }

        [Display(Name = "Nomor Dokumen")]
        public string? DocumentNumber { get; set; }

        [Display(Name = "Keterangan Dokumen")]
        public string? DocumentDescription { get; set; }

        // =================================================================
        // EXISTING AND NEW HELPER PROPERTIES
        // =================================================================

        public string FormattedFileSize => FormatFileSize(FileSize);
        public string DocumentDisplayName => GetDocumentDisplayName(DocumentType);

        // NEW: Additional helper properties
        public string FormattedUploadDate => UploadDate.ToString("dd MMM yyyy HH:mm");
        public string FormattedDocumentDate => DocumentDate?.ToString("dd MMM yyyy") ?? "-";
        public string DisplayDocumentNumber => !string.IsNullOrEmpty(DocumentNumber) ? DocumentNumber : "-";
        public bool HasDocumentDetails => DocumentDate.HasValue || !string.IsNullOrEmpty(DocumentNumber);
        public string FileIcon => GetFileIcon(FileExtension);
        public bool CanDownload { get; set; } = true;
        public bool CanView { get; set; } = true;

        public string DocumentDetailsFormatted
        {
            get
            {
                var details = new List<string>();

                if (DocumentDate.HasValue)
                    details.Add($"Tgl: {DocumentDate.Value:dd/MM/yyyy}");

                if (!string.IsNullOrEmpty(DocumentNumber))
                    details.Add($"No: {DocumentNumber}");

                return details.Any() ? string.Join(" | ", details) : "";
            }
        }

        public string DocumentStatusBadge
        {
            get
            {
                if (HasDocumentDetails)
                    return "<span class='badge badge-success'>Lengkap</span>";

                var requiredTypes = new[] { "SURAT_PERMOHONAN", "REKOMENDASI_DINAS_PROV", "REKOMENDASI_DAERAH_TUJUAN" };
                if (requiredTypes.Contains(DocumentType))
                    return "<span class='badge badge-warning'>Perlu Detail</span>";

                return "<span class='badge badge-secondary'>Opsional</span>";
            }
        }

        public bool IsRequiredDocument
        {
            get
            {
                var requiredTypes = new[] { "SURAT_PERMOHONAN", "REKOMENDASI_DINAS_PROV", "REKOMENDASI_DAERAH_TUJUAN" };
                return requiredTypes.Contains(DocumentType);
            }
        }

        // =================================================================
        // HELPER METHODS
        // =================================================================

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
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

        private string GetFileIcon(string fileExtension)
        {
            return fileExtension?.ToLower() switch
            {
                ".pdf" => "fas fa-file-pdf text-danger",
                ".jpg" or ".jpeg" or ".png" => "fas fa-file-image text-primary",
                ".doc" or ".docx" => "fas fa-file-word text-primary",
                ".xls" or ".xlsx" => "fas fa-file-excel text-success",
                _ => "fas fa-file text-secondary"
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

        // NEW: Document-related context
        public bool DocumentDetailsWereComplete { get; set; }
        public string? DocumentValidationNotes { get; set; }

        // Enhanced helper properties
        public string FormattedActionDate => ActionDate.ToString("dd MMM yyyy HH:mm");
        public string StatusText => PermitStatusHelper.GetStatusText(ToStatus);
        public string ActionIcon => GetActionIcon(Action);
        public string ActionClass => GetActionClass(Action);

        private string GetActionIcon(string action)
        {
            return action?.ToLower() switch
            {
                "approve" or "approved" => "fas fa-check-circle",
                "reject" or "rejected" => "fas fa-times-circle",
                "submit" or "submitted" => "fas fa-paper-plane",
                "review" => "fas fa-eye",
                _ => "fas fa-info-circle"
            };
        }

        private string GetActionClass(string action)
        {
            return action?.ToLower() switch
            {
                "approve" or "approved" => "text-success",
                "reject" or "rejected" => "text-danger",
                "submit" or "submitted" => "text-primary",
                "review" => "text-info",
                _ => "text-secondary"
            };
        }
    }

    public class DashboardStatsViewModel
    {
        public int TotalApplications { get; set; }
        public int PendingVerification { get; set; }
        public int PendingKepalaDinas { get; set; }
        public int ApprovedThisMonth { get; set; }
        public int RejectedThisMonth { get; set; }
        public int PendingAdminReview { get; set; }
        public int PendingVerifikatorReview { get; set; }
        public int AdminApprovedThisMonth { get; set; }
        public int VerifikatorApprovedThisMonth { get; set; }
        public List<PermitListViewModel> RecentApplications { get; set; } = new List<PermitListViewModel>();
        public List<PermitListViewModel> MyPendingApprovals { get; set; } = new List<PermitListViewModel>();

        // NEW: Document-related statistics
        public int ApplicationsWithIncompleteDocuments { get; set; }
        public int DocumentsRequiringDetails { get; set; }
        public double DocumentCompletionRate
        {
            get
            {
                if (TotalApplications == 0) return 0;
                var completeApplications = TotalApplications - ApplicationsWithIncompleteDocuments;
                return Math.Round((double)completeApplications / TotalApplications * 100, 1);
            }
        }
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

    public class DocumentDetailsViewModel
    {
        public int DocumentId { get; set; }

        [Display(Name = "Nama Dokumen")]
        public string DocumentName { get; set; } = string.Empty;

        [Display(Name = "Tipe Dokumen")]
        public string DocumentType { get; set; } = string.Empty;

        [Display(Name = "Tanggal Dokumen")]
        [DataType(DataType.Date)]
        public DateTime? DocumentDate { get; set; }

        [Display(Name = "Nomor Dokumen")]
        [StringLength(50, ErrorMessage = "Nomor dokumen maksimal 50 karakter")]
        public string? DocumentNumber { get; set; }

        [Display(Name = "Keterangan")]
        [StringLength(500, ErrorMessage = "Keterangan maksimal 500 karakter")]
        public string? DocumentDescription { get; set; }

        public bool IsRequired => GetRequiredDocumentTypes().Contains(DocumentType);

        public string ValidationMessage
        {
            get
            {
                if (!IsRequired) return string.Empty;

                var errors = new List<string>();
                if (!DocumentDate.HasValue)
                    errors.Add("Tanggal dokumen harus diisi");
                if (string.IsNullOrEmpty(DocumentNumber))
                    errors.Add("Nomor dokumen harus diisi");

                return string.Join(", ", errors);
            }
        }

        public bool IsValid => !IsRequired || (DocumentDate.HasValue && !string.IsNullOrEmpty(DocumentNumber));

        private static string[] GetRequiredDocumentTypes()
        {
            return new[] { "SURAT_PERMOHONAN", "REKOMENDASI_DINAS_PROV", "REKOMENDASI_DAERAH_TUJUAN" };
        }
    }

    public class BulkDocumentDetailsViewModel
    {
        public int PermitId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public List<DocumentDetailsViewModel> DocumentDetails { get; set; } = new List<DocumentDetailsViewModel>();

        public bool AllRequiredDocumentsHaveDetails =>
            DocumentDetails.Where(d => d.IsRequired).All(d => d.DocumentDate.HasValue && !string.IsNullOrEmpty(d.DocumentNumber));

        public int RequiredDocumentsCount => DocumentDetails.Count(d => d.IsRequired);
        public int CompletedRequiredDocumentsCount => DocumentDetails.Count(d => d.IsRequired && d.IsValid);

        public string CompletionStatus => $"{CompletedRequiredDocumentsCount}/{RequiredDocumentsCount} dokumen wajib lengkap";

        public double CompletionPercentage
        {
            get
            {
                if (RequiredDocumentsCount == 0) return 100;
                return Math.Round((double)CompletedRequiredDocumentsCount / RequiredDocumentsCount * 100, 1);
            }
        }
    }

    public class DocumentUploadResultViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UploadedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<DocumentViewModel> UploadedDocuments { get; set; } = new List<DocumentViewModel>();

        public string Summary => Success
            ? $"Berhasil upload {UploadedCount} dokumen"
            : $"Gagal upload: {Message}";
    }


    public class EditableLivestockDetailViewModel
    {
        public int? Id { get; set; } // For existing livestock details
        public int Index { get; set; } // For form binding

        [Display(Name = "Jenis Ternak")]
        public string LivestockType { get; set; }

        [Display(Name = "Jumlah")]
        public int Quantity { get; set; }

        [Display(Name = "Keterangan")]
        public string Description { get; set; }

        public bool IsMarkedForDeletion { get; set; }
        public bool IsNewEntry { get; set; }
    }
}
