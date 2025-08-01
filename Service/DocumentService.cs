using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.Service;
using PerizinanPeternakan.ViewModels;
using System.Text.RegularExpressions;

namespace PerizinanPeternakan.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DocumentService> _logger;

        // Constants
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private const long MIN_FILE_SIZE = 1024; // 1KB
        private static readonly string[] ALLOWED_EXTENSIONS = { ".pdf", ".jpg", ".jpeg", ".png" };
        private static readonly string[] ALLOWED_MIME_TYPES = {
            "application/pdf", "image/jpeg", "image/jpg", "image/png", "image/pjpeg"
        };

        public DocumentService(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<DocumentService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task<DocumentUploadResult> UploadSupportingDocumentsAsync(int permitId, PermitApplicationViewModel model, int userId)
        {
            var uploadedFiles = new List<string>();

            try
            {
                _logger.LogInformation("Starting document upload process for permit ID: {PermitId}", permitId);

                var documentsToUpload = GetDocumentsToUpload(model);

                // Create upload directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath, "documents", "supporting");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                    _logger.LogInformation("Created upload directory: {UploadsPath}", uploadsPath);
                }

                int uploadedCount = 0;
                var uploadedDocuments = new List<PermitDocument>();

                // Process each document
                foreach (var (file, type, name, date, number) in documentsToUpload)
                {
                    if (file != null && file.Length > 0)
                    {
                        // Validate file
                        var validationResult = ValidateUploadedFile(file);
                        if (!validationResult.IsValid)
                        {
                            CleanupUploadedFiles(uploadedFiles);
                            return new DocumentUploadResult
                            {
                                Success = false,
                                ErrorMessage = $"File {name} tidak valid: {string.Join(", ", validationResult.Errors)}",
                                UploadedCount = 0
                            };
                        }

                        // Generate unique filename and save file
                        var filePath = await SaveFileAsync(file, permitId, type, uploadsPath);
                        if (filePath == null)
                        {
                            CleanupUploadedFiles(uploadedFiles);
                            return new DocumentUploadResult
                            {
                                Success = false,
                                ErrorMessage = $"Gagal menyimpan file {name}",
                                UploadedCount = 0
                            };
                        }

                        uploadedFiles.Add(filePath);

                        // Create document record
                        var document = new PermitDocument
                        {
                            PermitApplicationId = permitId,
                            DocumentName = name,
                            FilePath = GetRelativeFilePath(filePath),
                            DocumentType = type,
                            FileSize = file.Length,
                            FileExtension = Path.GetExtension(file.FileName),
                            UploadedByUserId = userId,
                            UploadDate = DateTime.Now,
                            DocumentDate = date,
                            DocumentNumber = number?.Trim(),
                            DocumentDescription = GenerateDocumentDescription(name, number, date)
                        };

                        uploadedDocuments.Add(document);
                        uploadedCount++;

                        _logger.LogInformation("Processed document: {DocumentName} for permit {PermitId}", name, permitId);
                    }
                }

                if (!uploadedDocuments.Any())
                {
                    return new DocumentUploadResult
                    {
                        Success = false,
                        ErrorMessage = "Tidak ada dokumen yang berhasil diupload",
                        UploadedCount = 0
                    };
                }

                // Save to database
                try
                {
                    _context.PermitDocuments.AddRange(uploadedDocuments);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully uploaded {Count} documents for permit {PermitId}", uploadedCount, permitId);

                    return new DocumentUploadResult
                    {
                        Success = true,
                        ErrorMessage = string.Empty,
                        UploadedCount = uploadedCount,
                        UploadedDocuments = uploadedDocuments
                    };
                }
                catch (Exception dbEx)
                {
                    CleanupUploadedFiles(uploadedFiles);
                    _logger.LogError(dbEx, "Failed to save document information to database for permit {PermitId}", permitId);

                    return new DocumentUploadResult
                    {
                        Success = false,
                        ErrorMessage = $"Gagal menyimpan informasi dokumen ke database: {dbEx.Message}",
                        UploadedCount = 0
                    };
                }
            }
            catch (Exception ex)
            {
                CleanupUploadedFiles(uploadedFiles);
                _logger.LogError(ex, "Error during document upload for permit {PermitId}", permitId);

                return new DocumentUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Terjadi kesalahan saat upload dokumen: {ex.Message}",
                    UploadedCount = 0
                };
            }
        }

        public DocumentValidationResult ValidateUploadedFile(IFormFile file)
        {
            var errors = new List<string>();

            if (file == null || file.Length == 0)
            {
                errors.Add("File tidak boleh kosong");
                return new DocumentValidationResult { IsValid = false, Errors = errors };
            }

            // Check file size
            if (file.Length > MAX_FILE_SIZE)
            {
                errors.Add($"Ukuran file terlalu besar. Maksimal {FormatFileSize(MAX_FILE_SIZE)} (saat ini: {FormatFileSize(file.Length)})");
            }

            if (file.Length < MIN_FILE_SIZE)
            {
                errors.Add($"File terlalu kecil atau rusak. Minimal {FormatFileSize(MIN_FILE_SIZE)}");
            }

            // Check file extension
            var fileExtension = Path.GetExtension(file.FileName)?.ToLower();
            if (string.IsNullOrEmpty(fileExtension) || !ALLOWED_EXTENSIONS.Contains(fileExtension))
            {
                errors.Add($"Format file tidak didukung. Gunakan: {string.Join(", ", ALLOWED_EXTENSIONS)}");
            }

            // Check MIME type
            var contentType = file.ContentType?.ToLower();
            if (string.IsNullOrEmpty(contentType) || !ALLOWED_MIME_TYPES.Contains(contentType))
            {
                errors.Add($"Tipe file tidak valid (MIME: {contentType})");
            }

            // Check filename security
            var fileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                errors.Add("Nama file tidak valid");
            }
            else
            {
                if (ContainsDangerousCharacters(fileName))
                {
                    errors.Add("Nama file mengandung karakter yang tidak diizinkan");
                }

                if (ContainsDangerousExtensions(fileName))
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

            return new DocumentValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        public DocumentValidationResult ValidateAllRequiredDocuments(PermitApplicationViewModel model)
        {
            var errors = new List<string>();
            var requiredDocuments = new[]
            {
                (model.SuratPermohonan, "Surat Permohonan"),
                (model.RekomendasiDinasProv, "Rekomendasi Dinas Peternakan Provinsi NTB"),
                (model.RekomendasiDaerahTujuan, "Rekomendasi Pemasukan Ternak dari Daerah Tujuan"),
                (model.SKKHKabupatenAsal, "SKKH dari Kabupaten Asal"),
                (model.SKKHDinasProvinsi, "SKKH dari Dinas Peternakan Provinsi NTB"),
                (model.SuratJalanTernak, "Surat Keterangan Jalan Ternak/Rekomendasi Asal"),
                (model.HasilPemeriksaanFisik, "Hasil Pemeriksaan Fisik (Holding Ground)")
            };

            int missingCount = 0;

            foreach (var (file, documentName) in requiredDocuments)
            {
                if (file == null || file.Length == 0)
                {
                    errors.Add($"{documentName} wajib diupload");
                    missingCount++;
                }
                else
                {
                    var fileValidation = ValidateUploadedFile(file);
                    if (!fileValidation.IsValid)
                    {
                        errors.Add($"{documentName}: {string.Join(", ", fileValidation.Errors)}");
                    }
                }
            }

            if (missingCount > 0)
            {
                errors.Insert(0, $"Total dokumen yang belum diupload: {missingCount} dari {requiredDocuments.Length}");
            }

            return new DocumentValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        public DocumentValidationResult ValidateDocumentDetails(PermitApplicationViewModel model)
        {
            var errors = new List<string>();

            try
            {
                // Validate Surat Permohonan details
                if (model.SuratPermohonan != null && model.SuratPermohonan.Length > 0)
                {
                    ValidateDocumentDetailFields(
                        "Surat Permohonan",
                        model.SuratPermohonanTanggal,
                        model.SuratPermohonanNomor,
                        errors
                    );
                }

                // Validate Rekomendasi Dinas Provinsi details
                if (model.RekomendasiDinasProv != null && model.RekomendasiDinasProv.Length > 0)
                {
                    ValidateDocumentDetailFields(
                        "Rekomendasi Dinas Provinsi",
                        model.RekomendasiDinasProvTanggal,
                        model.RekomendasiDinasProvNomor,
                        errors
                    );
                }

                // Validate Rekomendasi Daerah Tujuan details
                if (model.RekomendasiDaerahTujuan != null && model.RekomendasiDaerahTujuan.Length > 0)
                {
                    ValidateDocumentDetailFields(
                        "Rekomendasi Daerah Tujuan",
                        model.RekomendasiDaerahTujuanTanggal,
                        model.RekomendasiDaerahTujuanNomor,
                        errors
                    );
                }

                // Validate Dokumen Opsional details (if provided)
                if (model.DokumenOpsional != null && model.DokumenOpsional.Length > 0)
                {
                    if (model.DokumenOpsionalTanggal.HasValue && model.DokumenOpsionalTanggal.Value > DateTime.Today)
                    {
                        errors.Add("Tanggal dokumen opsional tidak boleh di masa depan");
                    }

                    if (!string.IsNullOrWhiteSpace(model.DokumenOpsionalNomor) && !IsValidDocumentNumber(model.DokumenOpsionalNomor))
                    {
                        errors.Add("Format nomor dokumen opsional tidak valid");
                    }

                    if (string.IsNullOrWhiteSpace(model.DokumenOpsionalNama))
                    {
                        errors.Add("Nama dokumen opsional harus diisi jika mengupload dokumen");
                    }
                }

                return new DocumentValidationResult
                {
                    IsValid = errors.Count == 0,
                    Errors = errors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating document details");
                return new DocumentValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "Terjadi kesalahan saat validasi dokumen" }
                };
            }
        }

        public async Task<PermitDocument?> GetDocumentWithAuthorizationAsync(int documentId, int userId, string userRole)
        {
            var document = await _context.PermitDocuments
                .Include(d => d.PermitApplication)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null) return null;

            // Check authorization
            bool canAccess = userRole switch
            {
                "User" => document.PermitApplication.UserId == userId,
                "Admin" or "Verifikator" or "KepalaDinas" => true,
                _ => false
            };

            return canAccess ? document : null;
        }

        public async Task<int> CleanupOrphanedFilesAsync()
        {
            try
            {
                var supportingPath = Path.Combine(_environment.WebRootPath, "documents", "supporting");
                if (!Directory.Exists(supportingPath)) return 0;

                var files = Directory.GetFiles(supportingPath);
                var dbDocuments = await _context.PermitDocuments.Select(d => d.FilePath).ToListAsync();

                int cleanedCount = 0;

                foreach (var file in files)
                {
                    var relativePath = "/" + Path.GetRelativePath(_environment.WebRootPath, file).Replace("\\", "/");

                    if (!dbDocuments.Contains(relativePath))
                    {
                        try
                        {
                            File.Delete(file);
                            cleanedCount++;
                            _logger.LogInformation("Deleted orphaned file: {FileName}", Path.GetFileName(file));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete orphaned file: {FileName}", Path.GetFileName(file));
                        }
                    }
                }

                return cleanedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during orphaned files cleanup");
                return 0;
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

        public async Task<DocumentUpdateResult> UpdateDocumentDetailsAsync(int documentId, DateTime? documentDate, string? documentNumber, string? documentDescription)
        {
            try
            {
                var document = await _context.PermitDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
                if (document == null)
                {
                    return new DocumentUpdateResult
                    {
                        Success = false,
                        ErrorMessage = "Dokumen tidak ditemukan"
                    };
                }

                // Validate input
                if (documentDate.HasValue && documentDate.Value > DateTime.Today)
                {
                    return new DocumentUpdateResult
                    {
                        Success = false,
                        ErrorMessage = "Tanggal dokumen tidak boleh di masa depan"
                    };
                }

                if (!string.IsNullOrEmpty(documentNumber) && !IsValidDocumentNumber(documentNumber))
                {
                    return new DocumentUpdateResult
                    {
                        Success = false,
                        ErrorMessage = "Format nomor dokumen tidak valid"
                    };
                }

                // Update document
                document.DocumentDate = documentDate;
                document.DocumentNumber = documentNumber?.Trim();
                document.DocumentDescription = documentDescription?.Trim();

                await _context.SaveChangesAsync();

                return new DocumentUpdateResult
                {
                    Success = true,
                    ErrorMessage = string.Empty,
                    UpdatedDocument = document
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document details for document {DocumentId}", documentId);
                return new DocumentUpdateResult
                {
                    Success = false,
                    ErrorMessage = $"Terjadi kesalahan: {ex.Message}"
                };
            }
        }

        #region Private Helper Methods

        private IEnumerable<(IFormFile File, string Type, string Name, DateTime? Date, string? Number)> GetDocumentsToUpload(PermitApplicationViewModel model)
        {
            return new[]
            {
                (model.SuratPermohonan, "SURAT_PERMOHONAN", "Surat Permohonan", model.SuratPermohonanTanggal, model.SuratPermohonanNomor),
                (model.RekomendasiDinasProv, "REKOMENDASI_DINAS_PROV", "Rekomendasi Dinas Peternakan Provinsi NTB", model.RekomendasiDinasProvTanggal, model.RekomendasiDinasProvNomor),
                (model.RekomendasiDaerahTujuan, "REKOMENDASI_DAERAH_TUJUAN", "Rekomendasi Pemasukan Ternak dari Daerah Tujuan", model.RekomendasiDaerahTujuanTanggal, model.RekomendasiDaerahTujuanNomor),
                (model.SKKHKabupatenAsal, "SKKH_KABUPATEN_ASAL", "SKKH dari Kabupaten Asal", (DateTime?)null, (string?)null),
                (model.SKKHDinasProvinsi, "SKKH_DINAS_PROVINSI", "SKKH dari Dinas Peternakan Provinsi NTB", (DateTime?)null, (string?)null),
                (model.SuratJalanTernak, "SURAT_JALAN_TERNAK", "Surat Keterangan Jalan Ternak/Rekomendasi Asal", (DateTime?)null, (string?)null),
                (model.HasilPemeriksaanFisik, "HASIL_PEMERIKSAAN_FISIK", "Hasil Pemeriksaan Fisik (Holding Ground)", (DateTime?)null, (string?)null),
                (model.DokumenOpsional, "DOKUMEN_OPSIONAL", model.DokumenOpsionalNama ?? "Dokumen Opsional", model.DokumenOpsionalTanggal, model.DokumenOpsionalNomor)
            };
        }

        private async Task<string?> SaveFileAsync(IFormFile file, int permitId, string type, string uploadsPath)
        {
            try
            {
                var fileExtension = Path.GetExtension(file.FileName);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var uniqueFileName = $"{permitId}_{type}_{timestamp}_{Guid.NewGuid().ToString("N")[..8]}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file for permit {PermitId}, type {Type}", permitId, type);
                return null;
            }
        }

        private string GetRelativeFilePath(string fullPath)
        {
            var webRootPath = _environment.WebRootPath;
            var relativePath = "/" + Path.GetRelativePath(webRootPath, fullPath).Replace("\\", "/");
            return relativePath;
        }

        private void CleanupUploadedFiles(List<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _logger.LogInformation("Cleaned up file: {FilePath}", filePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup file: {FilePath}", filePath);
                }
            }
        }

        private void ValidateDocumentDetailFields(string documentName, DateTime? date, string? number, List<string> errors)
        {
            if (!date.HasValue)
            {
                errors.Add($"Tanggal pengajuan {documentName} harus diisi");
            }
            else if (date.Value > DateTime.Today)
            {
                errors.Add($"Tanggal pengajuan {documentName} tidak boleh di masa depan");
            }

            if (string.IsNullOrWhiteSpace(number))
            {
                errors.Add($"Nomor dokumen {documentName} harus diisi");
            }
            else if (!IsValidDocumentNumber(number))
            {
                errors.Add($"Format nomor dokumen {documentName} tidak valid");
            }
        }

        private bool IsValidDocumentNumber(string documentNumber)
        {
            if (string.IsNullOrWhiteSpace(documentNumber)) return false;
            if (documentNumber.Length > 50) return false;

            var validPattern = @"^[a-zA-Z0-9\/\-\._\s]+$";
            if (!Regex.IsMatch(documentNumber, validPattern)) return false;

            return !string.IsNullOrWhiteSpace(documentNumber.Trim());
        }

        private string GenerateDocumentDescription(string name, string? number, DateTime? date)
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

        private bool ContainsDangerousCharacters(string fileName)
        {
            var dangerousPatterns = new[] { "..", "/", "\\", ":", "*", "?", "\"", "<", ">", "|" };
            return dangerousPatterns.Any(pattern => fileName.Contains(pattern));
        }

        private bool ContainsDangerousExtensions(string fileName)
        {
            var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".scr", ".vbs", ".js" };
            var fullFileName = fileName.ToLower();
            return dangerousExtensions.Any(ext => fullFileName.Contains(ext));
        }

        private bool IsValidImageFile(IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var buffer = new byte[8];
                stream.Read(buffer, 0, 8);

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