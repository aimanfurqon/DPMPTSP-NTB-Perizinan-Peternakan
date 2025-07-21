using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;

namespace PerizinanPeternakan.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IPdfGeneratorService _pdfGenerator;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IPdfGeneratorService pdfGenerator,
            ILogger<DocumentService> logger)
        {
            _context = context;
            _environment = environment;
            _pdfGenerator = pdfGenerator;
            _logger = logger;
        }

        public async Task<(bool Success, string ErrorMessage, int UploadedCount)> UploadSupportingDocumentsAsync(
            int permitId, PermitApplicationViewModel model, int userId)
        {
            var uploadedFiles = new List<string>();

            try
            {
                _logger.LogInformation("Starting document upload process for permit ID: {PermitId}", permitId);

                var documentsToUpload = new[]
                {
                    (File: model.SuratPermohonan, Type: "SURAT_PERMOHONAN", Name: "Surat Permohonan",
                     Date: model.SuratPermohonanTanggal, Number: model.SuratPermohonanNomor),

                    (File: model.RekomendasiDinasProv, Type: "REKOMENDASI_DINAS_PROV", Name: "Rekomendasi Dinas Peternakan Provinsi NTB",
                     Date: model.RekomendasiDinasProvTanggal, Number: model.RekomendasiDinasProvNomor),

                    (File: model.RekomendasiDaerahTujuan, Type: "REKOMENDASI_DAERAH_TUJUAN", Name: "Rekomendasi Pemasukan Ternak dari Daerah Tujuan",
                     Date: model.RekomendasiDaerahTujuanTanggal, Number: model.RekomendasiDaerahTujuanNomor),

                    (File: model.SKKHKabupatenAsal, Type: "SKKH_KABUPATEN_ASAL", Name: "SKKH dari Kabupaten Asal",
                     Date: (DateTime?)null, Number: (string?)null),

                    (File: model.SKKHDinasProvinsi, Type: "SKKH_DINAS_PROVINSI", Name: "SKKH dari Dinas Peternakan Provinsi NTB",
                     Date: (DateTime?)null, Number: (string?)null),

                    (File: model.SuratJalanTernak, Type: "SURAT_JALAN_TERNAK", Name: "Surat Keterangan Jalan Ternak/Rekomendasi Asal",
                     Date: (DateTime?)null, Number: (string?)null),

                    (File: model.HasilPemeriksaanFisik, Type: "HASIL_PEMERIKSAAN_FISIK", Name: "Hasil Pemeriksaan Fisik (Holding Ground)",
                     Date: (DateTime?)null, Number: (string?)null)
                };

                // Create upload directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath, "documents", "supporting");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                    _logger.LogInformation("Created upload directory: {Path}", uploadsPath);
                }

                int uploadedCount = 0;
                var uploadedDocuments = new List<PermitDocument>();

                foreach (var (file, type, name, date, number) in documentsToUpload)
                {
                    if (file != null && file.Length > 0)
                    {
                        _logger.LogInformation("Processing document: {Name}", name);

                        // Validate file
                        var validation = ValidateUploadedFile(file);
                        if (!validation.IsValid)
                        {
                            CleanupUploadedFiles(uploadedFiles);
                            return (false, $"File {name} tidak valid: {string.Join(", ", validation.Errors)}", uploadedCount);
                        }

                        // Generate unique filename
                        var fileExtension = Path.GetExtension(file.FileName);
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var uniqueFileName = $"{permitId}_{type}_{timestamp}_{Guid.NewGuid().ToString("N")[..8]}{fileExtension}";
                        var filePath = Path.Combine(uploadsPath, uniqueFileName);

                        // Save file to disk
                        try
                        {
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                            uploadedFiles.Add(filePath);
                            _logger.LogInformation("File saved: {FileName}", uniqueFileName);
                        }
                        catch (Exception fileEx)
                        {
                            _logger.LogError(fileEx, "Error saving file {Name}", name);
                            CleanupUploadedFiles(uploadedFiles);
                            return (false, $"Gagal menyimpan file {name}: {fileEx.Message}", uploadedCount);
                        }

                        // Create database record
                        var document = new PermitDocument
                        {
                            PermitApplicationId = permitId,
                            DocumentName = name,
                            FilePath = filePath,
                            DocumentType = type,
                            FileSize = file.Length,
                            FileExtension = fileExtension,
                            UploadedByUserId = userId,
                            UploadDate = DateTime.Now,
                            DocumentDate = date,
                            DocumentNumber = number,
                            DocumentDescription = GenerateDocumentDescription(name, number, date)
                        };

                        uploadedDocuments.Add(document);
                        uploadedCount++;
                    }
                }

                if (!uploadedDocuments.Any())
                {
                    return (false, "Tidak ada dokumen yang berhasil diupload", 0);
                }

                // Save all documents to database
                try
                {
                    _context.PermitDocuments.AddRange(uploadedDocuments);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Saved {Count} documents to database", uploadedDocuments.Count);
                    return (true, string.Empty, uploadedCount);
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Database error during document save");
                    CleanupUploadedFiles(uploadedFiles);
                    return (false, $"Gagal menyimpan informasi dokumen ke database: {dbEx.Message}", 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UploadSupportingDocuments");
                CleanupUploadedFiles(uploadedFiles);
                return (false, $"Terjadi kesalahan saat upload dokumen: {ex.Message}", 0);
            }
        }

        public (bool IsValid, List<string> Errors) ValidateUploadedFile(IFormFile file)
        {
            var errors = new List<string>();
            const int maxFileSize = 5 * 1024 * 1024; // 5MB
            const int minFileSize = 1024; // 1KB minimum

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var allowedMimeTypes = new[] {
                "application/pdf",
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/pjpeg"
            };

            if (file == null || file.Length == 0)
            {
                errors.Add("File tidak boleh kosong");
                return (false, errors);
            }

            // Check file size
            if (file.Length > maxFileSize)
            {
                errors.Add($"Ukuran file terlalu besar. Maksimal {FormatFileSize(maxFileSize)} (saat ini: {FormatFileSize(file.Length)})");
            }

            if (file.Length < minFileSize)
            {
                errors.Add($"File terlalu kecil atau rusak. Minimal {FormatFileSize(minFileSize)}");
            }

            // Check file extension
            var fileExtension = Path.GetExtension(file.FileName)?.ToLower();
            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                errors.Add($"Format file tidak didukung. Gunakan: {string.Join(", ", allowedExtensions)}");
            }

            // Check MIME type
            var contentType = file.ContentType?.ToLower();
            if (string.IsNullOrEmpty(contentType) || !allowedMimeTypes.Contains(contentType))
            {
                errors.Add($"Tipe file tidak valid (MIME: {contentType})");
            }

            // Check filename safety
            var fileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                errors.Add("Nama file tidak valid");
            }
            else
            {
                var dangerousPatterns = new[] { "..", "/", "\\", ":", "*", "?", "\"", "<", ">", "|" };
                if (dangerousPatterns.Any(pattern => fileName.Contains(pattern)))
                {
                    errors.Add("Nama file mengandung karakter yang tidak diizinkan");
                }

                var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".scr", ".vbs", ".js" };
                var fullFileName = fileName.ToLower();
                if (dangerousExtensions.Any(ext => fullFileName.Contains(ext)))
                {
                    errors.Add("File berpotensi berbahaya");
                }
            }

            // Additional security check for images
            if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png")
            {
                if (!IsValidImageFile(file))
                {
                    errors.Add("File gambar tidak valid atau rusak");
                }
            }

            return (errors.Count == 0, errors);
        }

        public bool IsValidFile(IFormFile file)
        {
            var validation = ValidateUploadedFile(file);
            return validation.IsValid;
        }

        public bool ValidateAllDocumentsUploaded(PermitApplicationViewModel model)
        {
            try
            {
                var requiredDocuments = new Dictionary<string, IFormFile?>
                {
                    { "Surat Permohonan", model.SuratPermohonan },
                    { "Rekomendasi Dinas Provinsi", model.RekomendasiDinasProv },
                    { "Rekomendasi Daerah Tujuan", model.RekomendasiDaerahTujuan },
                    { "SKKH Kabupaten Asal", model.SKKHKabupatenAsal },
                    { "SKKH Dinas Provinsi", model.SKKHDinasProvinsi },
                    { "Surat Jalan Ternak", model.SuratJalanTernak },
                    { "Hasil Pemeriksaan Fisik", model.HasilPemeriksaanFisik }
                };

                foreach (var doc in requiredDocuments)
                {
                    if (doc.Value == null || doc.Value.Length == 0)
                    {
                        _logger.LogWarning("Missing document: {DocumentName}", doc.Key);
                        return false;
                    }

                    var fileValidation = ValidateUploadedFile(doc.Value);
                    if (!fileValidation.IsValid)
                    {
                        _logger.LogWarning("Invalid document {DocumentName}: {Errors}",
                            doc.Key, string.Join(", ", fileValidation.Errors));
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating documents");
                return false;
            }
        }

        public bool ValidateDocumentDetails(PermitApplicationViewModel model)
        {
            try
            {
                var errors = new List<string>();

                // Validate Surat Permohonan details
                if (model.SuratPermohonan != null && model.SuratPermohonan.Length > 0)
                {
                    if (!model.SuratPermohonanTanggal.HasValue)
                    {
                        errors.Add("Tanggal pengajuan Surat Permohonan harus diisi");
                    }
                    else if (model.SuratPermohonanTanggal.Value > DateTime.Today)
                    {
                        errors.Add("Tanggal pengajuan Surat Permohonan tidak boleh di masa depan");
                    }

                    if (string.IsNullOrWhiteSpace(model.SuratPermohonanNomor))
                    {
                        errors.Add("Nomor dokumen Surat Permohonan harus diisi");
                    }
                    else if (!IsValidDocumentNumber(model.SuratPermohonanNomor))
                    {
                        errors.Add("Format nomor dokumen Surat Permohonan tidak valid");
                    }
                }

                // Validate Rekomendasi Dinas Provinsi details
                if (model.RekomendasiDinasProv != null && model.RekomendasiDinasProv.Length > 0)
                {
                    if (!model.RekomendasiDinasProvTanggal.HasValue)
                    {
                        errors.Add("Tanggal pengajuan Rekomendasi Dinas Provinsi harus diisi");
                    }
                    else if (model.RekomendasiDinasProvTanggal.Value > DateTime.Today)
                    {
                        errors.Add("Tanggal pengajuan Rekomendasi Dinas Provinsi tidak boleh di masa depan");
                    }

                    if (string.IsNullOrWhiteSpace(model.RekomendasiDinasProvNomor))
                    {
                        errors.Add("Nomor dokumen Rekomendasi Dinas Provinsi harus diisi");
                    }
                    else if (!IsValidDocumentNumber(model.RekomendasiDinasProvNomor))
                    {
                        errors.Add("Format nomor dokumen Rekomendasi Dinas Provinsi tidak valid");
                    }
                }

                // Validate Rekomendasi Daerah Tujuan details
                if (model.RekomendasiDaerahTujuan != null && model.RekomendasiDaerahTujuan.Length > 0)
                {
                    if (!model.RekomendasiDaerahTujuanTanggal.HasValue)
                    {
                        errors.Add("Tanggal pengajuan Rekomendasi Daerah Tujuan harus diisi");
                    }
                    else if (model.RekomendasiDaerahTujuanTanggal.Value > DateTime.Today)
                    {
                        errors.Add("Tanggal pengajuan Rekomendasi Daerah Tujuan tidak boleh di masa depan");
                    }

                    if (string.IsNullOrWhiteSpace(model.RekomendasiDaerahTujuanNomor))
                    {
                        errors.Add("Nomor dokumen Rekomendasi Daerah Tujuan harus diisi");
                    }
                    else if (!IsValidDocumentNumber(model.RekomendasiDaerahTujuanNomor))
                    {
                        errors.Add("Format nomor dokumen Rekomendasi Daerah Tujuan tidak valid");
                    }
                }

                if (errors.Any())
                {
                    _logger.LogWarning("Document details validation failed: {Errors}", string.Join("; ", errors));
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateDocumentDetails");
                return false;
            }
        }

        public async Task<DocumentViewModel?> GetDocumentAsync(int documentId, string userRole, int userId)
        {
            try
            {
                var document = await _context.PermitDocuments
                    .Include(d => d.PermitApplication)
                    .Include(d => d.UploadedByUser)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null) return null;

                // Check permission
                bool canAccess = false;
                if (userRole == "User" && document.PermitApplication.UserId == userId)
                {
                    canAccess = true;
                }
                else if (userRole == "Admin" || userRole == "Verifikator" || userRole == "KepalaDinas")
                {
                    canAccess = true;
                }

                if (!canAccess) return null;

                return new DocumentViewModel
                {
                    Id = document.Id,
                    DocumentName = document.DocumentName,
                    DocumentType = document.DocumentType,
                    FilePath = document.FilePath,
                    FileSize = document.FileSize,
                    FileExtension = document.FileExtension,
                    UploadDate = document.UploadDate,
                    UploadedBy = document.UploadedByUser.NamaLengkap,
                    DocumentDate = document.DocumentDate,
                    DocumentNumber = document.DocumentNumber,
                    DocumentDescription = document.DocumentDescription
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document {DocumentId}", documentId);
                return null;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> UpdateDocumentDetailsAsync(
            int documentId, DocumentDetailsViewModel model, int userId)
        {
            try
            {
                var document = await _context.PermitDocuments
                    .Include(d => d.PermitApplication)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return (false, "Dokumen tidak ditemukan");
                }

                // Update document details
                document.DocumentDate = model.DocumentDate;
                document.DocumentNumber = model.DocumentNumber;
                document.DocumentDescription = model.DocumentDescription;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Document details updated for document {DocumentId} by user {UserId}",
                    documentId, userId);

                return (true, "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document details for document {DocumentId}", documentId);
                return (false, "Terjadi kesalahan saat memperbarui detail dokumen");
            }
        }

        public async Task<(bool Success, string Message, int UpdatedCount)> BulkUpdateDocumentDetailsAsync(
            List<DocumentDetailsViewModel> documentDetails, int userId)
        {
            try
            {
                var documentIds = documentDetails.Select(d => d.DocumentId).ToList();
                var documents = await _context.PermitDocuments
                    .Where(d => documentIds.Contains(d.Id))
                    .ToListAsync();

                foreach (var docDetail in documentDetails)
                {
                    var document = documents.FirstOrDefault(d => d.Id == docDetail.DocumentId);
                    if (document != null)
                    {
                        document.DocumentDate = docDetail.DocumentDate;
                        document.DocumentNumber = docDetail.DocumentNumber;
                        document.DocumentDescription = docDetail.DocumentDescription;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Bulk updated {Count} document details by user {UserId}",
                    documents.Count, userId);

                return (true, $"Berhasil memperbarui detail {documents.Count} dokumen", documents.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk update document details by user {UserId}", userId);
                return (false, "Terjadi kesalahan saat memperbarui detail dokumen", 0);
            }
        }

        public async Task<(bool CanAccess, byte[]? FileBytes, string? ContentType, string? FileName)>
            GetDocumentForDownloadAsync(int documentId, string userRole, int userId)
        {
            try
            {
                var document = await _context.PermitDocuments
                    .Include(d => d.PermitApplication)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return (false, null, null, null);
                }

                // Check permission
                bool canDownload = false;
                if (userRole == "User" && document.PermitApplication.UserId == userId)
                {
                    canDownload = true;
                }
                else if (userRole == "Admin" || userRole == "Verifikator" || userRole == "KepalaDinas")
                {
                    canDownload = true;
                }

                if (!canDownload)
                {
                    return (false, null, null, null);
                }

                var filePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/'));

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File not found: {FilePath}", filePath);
                    return (false, null, null, null);
                }

                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var contentType = GetContentType(document.FileExtension);
                var fileName = $"{document.DocumentName}{document.FileExtension}";

                return (true, fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document for download {DocumentId}", documentId);
                return (false, null, null, null);
            }
        }

        public string GetContentType(string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }

        public async Task<string> GetDocumentContentAsync(LivestockPermitApplication permit)
        {
            try
            {
                if (!string.IsNullOrEmpty(permit.GeneratedDocumentPath))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                        permit.GeneratedDocumentPath.TrimStart('/'));

                    if (File.Exists(filePath))
                    {
                        var content = await File.ReadAllTextAsync(filePath);
                        return content;
                    }
                }

                // Generate fresh content if file doesn't exist
                var htmlBytes = await _pdfGenerator.GeneratePermitPdf(permit);
                return System.Text.Encoding.UTF8.GetString(htmlBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document content for permit {PermitId}", permit.Id);
                return "<div class='alert alert-danger'><i class='fas fa-exclamation-triangle'></i> Gagal memuat konten dokumen.</div>";
            }
        }

        public async Task<byte[]> GetDocumentFileAsync(int permitId, string userRole, int userId)
        {
            try
            {
                var permit = await _context.PermitApplications
                    .Include(p => p.User)
                    .Include(p => p.LivestockDetails)
                    .Include(p => p.Admin)
                    .Include(p => p.Verifikator)
                    .Include(p => p.KepalaDinas)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null)
                {
                    return Array.Empty<byte>();
                }

                // Check permission
                bool canView = false;
                switch (userRole)
                {
                    case "Admin":
                        canView = permit.Status >= PermitStatus.AdminApproved;
                        break;
                    case "Verifikator":
                        canView = permit.Status >= PermitStatus.AdminApproved;
                        break;
                    case "KepalaDinas":
                        canView = permit.Status >= PermitStatus.VerifikatorApproved;
                        break;
                    case "User":
                        canView = permit.UserId == userId;
                        break;
                }

                if (!canView)
                {
                    return Array.Empty<byte>();
                }

                // Generate document if doesn't exist
                if (string.IsNullOrEmpty(permit.GeneratedDocumentPath))
                {
                    var htmlBytes = await _pdfGenerator.GeneratePermitPdf(permit);

                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents", "permits");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    var fileName = $"permit_{permit.ApplicationNumber.Replace("/", "_")}_{DateTime.Now:yyyyMMddHHmmss}.html";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    await File.WriteAllBytesAsync(filePath, htmlBytes);

                    permit.GeneratedDocumentPath = $"/documents/permits/{fileName}";
                    await _context.SaveChangesAsync();
                }

                var documentPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    permit.GeneratedDocumentPath.TrimStart('/'));

                if (File.Exists(documentPath))
                {
                    return await File.ReadAllBytesAsync(documentPath);
                }

                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document file for permit {PermitId}", permitId);
                return Array.Empty<byte>();
            }
        }

        public string GenerateDocumentDescription(string name, string? number, DateTime? date)
        {
            try
            {
                var parts = new List<string> { $"Dokumen {name}" };

                if (!string.IsNullOrEmpty(number))
                    parts.Add($"nomor {number}");

                if (date.HasValue)
                    parts.Add($"tanggal {date.Value:dd/MM/yyyy}");

                return string.Join(" dengan ", parts);
            }
            catch
            {
                return $"Dokumen {name}";
            }
        }

        public bool IsValidDocumentNumber(string documentNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(documentNumber))
                    return false;

                // Check length (max 50 characters)
                if (documentNumber.Length > 50)
                    return false;

                // Check for valid characters (alphanumeric, slash, dash, dot, underscore, space)
                var validPattern = @"^[a-zA-Z0-9\/\-\._\s]+$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(documentNumber, validPattern))
                    return false;

                // Check that it's not just whitespace
                if (string.IsNullOrWhiteSpace(documentNumber.Trim()))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public string FormatFileSize(long bytes)
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

        public void CleanupUploadedFiles(List<string> filePaths)
        {
            try
            {
                foreach (var filePath in filePaths)
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _logger.LogInformation("Cleaned up file: {FileName}", Path.GetFileName(filePath));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }

        public async Task<List<object>> ValidateDocumentDetailsAPIAsync(List<DocumentDetailsViewModel> documentDetails)
        {
            try
            {
                var validationResults = new List<object>();

                foreach (var doc in documentDetails)
                {
                    var isValid = true;
                    var errors = new List<string>();

                    if (doc.IsRequired)
                    {
                        if (!doc.DocumentDate.HasValue)
                        {
                            errors.Add("Tanggal dokumen harus diisi");
                            isValid = false;
                        }

                        if (string.IsNullOrEmpty(doc.DocumentNumber))
                        {
                            errors.Add("Nomor dokumen harus diisi");
                            isValid = false;
                        }
                        else if (!IsValidDocumentNumber(doc.DocumentNumber))
                        {
                            errors.Add("Format nomor dokumen tidak valid");
                            isValid = false;
                        }
                    }

                    validationResults.Add(new
                    {
                        documentId = doc.DocumentId,
                        isValid = isValid,
                        errors = errors
                    });
                }

                return validationResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateDocumentDetailsAPI");
                return new List<object>();
            }
        }

        #region Private Helper Methods

        private bool IsValidImageFile(IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var buffer = new byte[8];
                stream.Read(buffer, 0, 8);

                // Check for common image file signatures
                // JPEG: FF D8 FF
                if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
                    return true;

                // PNG: 89 50 4E 47 0D 0A 1A 0A
                if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
                    buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}