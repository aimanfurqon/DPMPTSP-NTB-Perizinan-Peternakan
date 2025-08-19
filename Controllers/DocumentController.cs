using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.Service;
using PerizinanPeternakan.Services;
using PerizinanPeternakan.ViewModels;
using System.Text;

namespace PerizinanPeternakan.Controllers
{
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IDocumentService _documentService;

        public DocumentController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IDocumentService documentService)
        {
            _context = context;
            _environment = environment;
            _documentService = documentService;
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            var document = await _documentService.GetDocumentWithAuthorizationAsync(id, userId.Value, userRole);

            if (document == null)
            {
                TempData["ErrorMessage"] = "Dokumen tidak ditemukan atau Anda tidak memiliki akses";
                return NotFound();
            }

            bool canDownload = false;
            if (userRole == "User" && document.PermitApplication.UserId == userId.Value)
            {
                canDownload = true;
            }
            else if (userRole == "Admin" || userRole == "Verifikator" || userRole == "KepalaDinas")
            {
                canDownload = true;
            }

            if (!canDownload)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengunduh dokumen ini";
                return Forbid();
            }

            var filePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "File tidak ditemukan di server";
                return RedirectToAction("Detail", "Permit", new { id = document.PermitApplicationId });
            }

            try
            {
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var contentType = _documentService.GetContentType(document.FileExtension);
                var fileName = $"{document.DocumentName}{document.FileExtension}";

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mengunduh dokumen";
                return RedirectToAction("Detail", "Permit", new { id = document.PermitApplicationId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PreviewDocument(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            var document = await _documentService.GetDocumentWithAuthorizationAsync(id, userId.Value, userRole);

            if (document == null)
            {
                TempData["ErrorMessage"] = "Dokumen tidak ditemukan atau Anda tidak memiliki akses";
                return NotFound();
            }

            bool canPreview = false;
            if (userRole == "User" && document.PermitApplication.UserId == userId.Value)
            {
                canPreview = true;
            }
            else if (userRole == "Admin" || userRole == "Verifikator" || userRole == "KepalaDinas")
            {
                canPreview = true;
            }

            if (!canPreview)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk melihat dokumen ini";
                return Forbid();
            }

            var filePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "File tidak ditemukan di server";
                return NotFound();
            }

            try
            {
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var contentType = _documentService.GetContentType(document.FileExtension);
                
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat dokumen";
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditDocumentDetails(int documentId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin" && userRole != "Verifikator")
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengedit detail dokumen";
                return RedirectToAction("Index", "Permit");
            }

            var document = await _context.PermitDocuments
                .Include(d => d.PermitApplication)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                TempData["ErrorMessage"] = "Dokumen tidak ditemukan";
                return RedirectToAction("Index", "Permit");
            }

            var model = new DocumentDetailsViewModel
            {
                DocumentId = document.Id,
                DocumentName = document.DocumentName,
                DocumentType = document.DocumentType,
                DocumentDate = document.DocumentDate,
                DocumentNumber = document.DocumentNumber,
                DocumentDescription = document.DocumentDescription
            };

            ViewBag.PermitApplicationNumber = document.PermitApplication.ApplicationNumber;
            return View(model);
        }

        [HttpPost]
        [Route("Document/EditDocumentDetailsFromView")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDocumentDetailsFromView(DocumentDetailsViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin" && userRole != "Verifikator")
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengedit detail dokumen";
                return RedirectToAction("Index", "Permit");
            }

            if (!ModelState.IsValid)
            {
                return View("EditDocumentDetails", model);
            }

            try
            {
                var document = await _context.PermitDocuments
                    .Include(d => d.PermitApplication)
                    .FirstOrDefaultAsync(d => d.Id == model.DocumentId);

                if (document == null)
                {
                    TempData["ErrorMessage"] = "Dokumen tidak ditemukan";
                    return RedirectToAction("Index", "Permit");
                }

                document.DocumentDate = model.DocumentDate;
                document.DocumentNumber = model.DocumentNumber;
                document.DocumentDescription = model.DocumentDescription;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Detail dokumen berhasil diperbarui";
                return RedirectToAction("Detail", "Permit", new { id = document.PermitApplicationId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating document details: {ex.Message}");
                ModelState.AddModelError("", "Terjadi kesalahan saat memperbarui detail dokumen");
                return View("EditDocumentDetails", model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDocumentDetails([FromBody] EditDocumentDetailsRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Session tidak valid" });
            }

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin")
            {
                return Json(new { success = false, message = "Tidak memiliki akses untuk mengedit dokumen" });
            }

            var result = await _documentService.UpdateDocumentDetailsAsync(
                request.DocumentId,
                request.DocumentDate,
                request.DocumentNumber,
                request.DocumentDescription
            );

            if (result.Success)
            {
                return Json(new
                {
                    success = true,
                    message = "Detail dokumen berhasil disimpan",
                    data = new
                    {
                        documentId = result.UpdatedDocument.Id,
                        documentDate = result.UpdatedDocument.DocumentDate?.ToString("yyyy-MM-dd"),
                        documentNumber = result.UpdatedDocument.DocumentNumber,
                        documentDescription = result.UpdatedDocument.DocumentDescription
                    }
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkUpdateDocumentDetails(BulkDocumentDetailsViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin" && userRole != "Verifikator")
            {
                return Json(new { success = false, message = "Akses ditolak" });
            }

            try
            {
                var documentIds = model.DocumentDetails.Select(d => d.DocumentId).ToList();
                var documents = await _context.PermitDocuments
                    .Where(d => documentIds.Contains(d.Id))
                    .ToListAsync();

                foreach (var docDetail in model.DocumentDetails)
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

                return Json(new
                {
                    success = true,
                    message = $"Berhasil memperbarui detail {documents.Count} dokumen",
                    updatedCount = documents.Count
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in bulk update: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Terjadi kesalahan saat memperbarui detail dokumen"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDocument()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Json(new { success = false, message = "User tidak ditemukan" });

                var userRole = HttpContext.Session.GetString("Role");
                if (userRole != "Admin")
                {
                    return Json(new { success = false, message = "Akses ditolak. Hanya Admin yang dapat mengedit dokumen." });
                }

                var documentId = int.Parse(Request.Form["documentId"]);
                var permitId = int.Parse(Request.Form["permitId"]);
                var documentType = Request.Form["documentType"].ToString();
                var documentNumber = Request.Form["documentNumber"].ToString();
                var documentDateStr = Request.Form["documentDate"].ToString();
                var documentDescription = Request.Form["documentDescription"].ToString();
                var documentFile = Request.Form.Files["documentFile"];

                var permit = await _context.PermitApplications
                    .Include(p => p.Documents)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null)
                {
                    return Json(new { success = false, message = "Permohonan tidak ditemukan" });
                }

                var document = permit.Documents.FirstOrDefault(d => d.Id == documentId);
                if (document == null)
                {
                    return Json(new { success = false, message = "Dokumen tidak ditemukan" });
                }

                document.DocumentType = documentType;
                
                var noNumberDateTypes = new[] { "SKKH_KABUPATEN_ASAL", "SKKH_DINAS_PROVINSI", "SURAT_JALAN_TERNAK", "HASIL_PEMERIKSAAN_FISIK" };
                
                if (noNumberDateTypes.Contains(documentType))
                {
                    document.DocumentNumber = null;
                    document.DocumentDate = null;
                }
                else
                {
                    document.DocumentNumber = string.IsNullOrEmpty(documentNumber) ? null : documentNumber;
                    
                    DateTime? documentDate = null;
                    if (!string.IsNullOrEmpty(documentDateStr) && DateTime.TryParse(documentDateStr, out DateTime parsedDate))
                    {
                        documentDate = parsedDate;
                    }
                    document.DocumentDate = documentDate;
                }
                
                document.DocumentDescription = string.IsNullOrEmpty(documentDescription) ? null : documentDescription;
                document.UploadedByUserId = GetCurrentUserId() ?? 0;
                document.UploadDate = DateTime.Now;

                if (documentFile != null && documentFile.Length > 0)
                {
                    var validation = ValidateUploadedFile(documentFile);
                    if (!validation.IsValid)
                    {
                        return Json(new { success = false, message = string.Join(", ", validation.Errors) });
                    }

                    if (!string.IsNullOrEmpty(document.FilePath) && System.IO.File.Exists(document.FilePath))
                    {
                        System.IO.File.Delete(document.FilePath);
                    }

                    var fileName = SanitizeFileName(documentFile.FileName);
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var uniqueFileName = $"{documentType}_{timestamp}_{fileName}";
                    var filePath = Path.Combine(_environment.WebRootPath, "documents", "supporting", uniqueFileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await documentFile.CopyToAsync(stream);
                    }

                    document.FilePath = $"/documents/supporting/{uniqueFileName}";
                    document.DocumentName = fileName;
                    document.FileSize = documentFile.Length;
                    document.FileExtension = Path.GetExtension(fileName);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Dokumen berhasil diperbarui" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddDocument()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Json(new { success = false, message = "User tidak ditemukan" });

                var userRole = HttpContext.Session.GetString("Role");
                if (userRole != "Admin")
                {
                    return Json(new { success = false, message = "Akses ditolak. Hanya Admin yang dapat menambahkan dokumen." });
                }

                var permitId = int.Parse(Request.Form["permitId"]);
                var documentType = Request.Form["documentType"].ToString();
                var documentNumber = Request.Form["documentNumber"].ToString();
                var documentDateStr = Request.Form["documentDate"].ToString();
                var documentDescription = Request.Form["documentDescription"].ToString();
                var documentFile = Request.Form.Files["documentFile"];

                if (documentFile == null || documentFile.Length == 0)
                {
                    return Json(new { success = false, message = "File dokumen wajib diupload" });
                }

                var permit = await _context.PermitApplications
                    .Include(p => p.Documents)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null)
                {
                    return Json(new { success = false, message = "Permohonan tidak ditemukan" });
                }

                var validation = ValidateUploadedFile(documentFile);
                if (!validation.IsValid)
                {
                    return Json(new { success = false, message = string.Join(", ", validation.Errors) });
                }

                if (permit.Documents.Any(d => d.DocumentType == documentType))
                {
                    return Json(new { success = false, message = $"Dokumen jenis {documentType} sudah ada" });
                }

                var fileName = SanitizeFileName(documentFile.FileName);
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var uniqueFileName = $"{documentType}_{timestamp}_{fileName}";
                var filePath = Path.Combine(_environment.WebRootPath, "documents", "supporting", uniqueFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await documentFile.CopyToAsync(stream);
                }

                var noNumberDateTypes = new[] { "SKKH_KABUPATEN_ASAL", "SKKH_DINAS_PROVINSI", "SURAT_JALAN_TERNAK", "HASIL_PEMERIKSAAN_FISIK" };
                
                var document = new PermitDocument
                {
                    PermitApplicationId = permitId,
                    DocumentType = documentType,
                    DocumentName = fileName,
                    DocumentNumber = noNumberDateTypes.Contains(documentType) ? null : (string.IsNullOrEmpty(documentNumber) ? null : documentNumber),
                    DocumentDescription = string.IsNullOrEmpty(documentDescription) ? null : documentDescription,
                    DocumentDate = noNumberDateTypes.Contains(documentType) ? null : 
                        (string.IsNullOrEmpty(documentDateStr) ? null : 
                        (DateTime.TryParse(documentDateStr, out DateTime parsedDate) ? parsedDate : null)),
                    FilePath = $"/documents/supporting/{uniqueFileName}",
                    FileSize = documentFile.Length,
                    FileExtension = Path.GetExtension(fileName),
                    UploadedByUserId = GetCurrentUserId() ?? 0,
                    UploadDate = DateTime.Now
                };

                _context.PermitDocuments.Add(document);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Dokumen berhasil ditambahkan" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDocument()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Json(new { success = false, message = "User tidak ditemukan" });

                var userRole = HttpContext.Session.GetString("Role");
                if (userRole != "Admin")
                {
                    return Json(new { success = false, message = "Akses ditolak. Hanya Admin yang dapat menghapus dokumen." });
                }

                var documentId = int.Parse(Request.Form["documentId"]);
                var permitId = int.Parse(Request.Form["permitId"]);

                var permit = await _context.PermitApplications
                    .Include(p => p.Documents)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null)
                {
                    return Json(new { success = false, message = "Permohonan tidak ditemukan" });
                }

                var document = permit.Documents.FirstOrDefault(d => d.Id == documentId);
                if (document == null)
                {
                    return Json(new { success = false, message = "Dokumen tidak ditemukan" });
                }

                if (!string.IsNullOrEmpty(document.FilePath))
                {
                    var fullPath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                _context.PermitDocuments.Remove(document);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Dokumen berhasil dihapus" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        #region Private Methods

        private int? GetCurrentUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "unknown";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            sanitized = sanitized.Replace(" ", "_")
                                .Replace("..", "_")
                                .Replace("__", "_")
                                .Trim('_');

            return string.IsNullOrWhiteSpace(sanitized) ? "document" : sanitized;
        }

        private (bool IsValid, List<string> Errors) ValidateUploadedFile(IFormFile file)
        {
            var errors = new List<string>();
            const int maxFileSize = 5 * 1024 * 1024; 
            const int minFileSize = 1024; 

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

            if (file.Length > maxFileSize)
            {
                errors.Add($"Ukuran file terlalu besar. Maksimal {FormatFileSize(maxFileSize)} (saat ini: {FormatFileSize(file.Length)})");
            }

            if (file.Length < minFileSize)
            {
                errors.Add($"File terlalu kecil atau rusak. Minimal {FormatFileSize(minFileSize)}");
            }

            var fileExtension = Path.GetExtension(file.FileName)?.ToLower();
            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                errors.Add($"Format file tidak didukung. Gunakan: {string.Join(", ", allowedExtensions)}");
            }

            var contentType = file.ContentType?.ToLower();
            if (string.IsNullOrEmpty(contentType) || !allowedMimeTypes.Contains(contentType))
            {
                errors.Add($"Tipe file tidak valid (MIME: {contentType})");
            }

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

            if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png")
            {
                if (!IsValidImageFile(file))
                {
                    errors.Add("File gambar tidak valid atau rusak");
                }
            }

            return (errors.Count == 0, errors);
        }

        private bool IsValidImageFile(IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var buffer = new byte[8];
                stream.Read(buffer, 0, 8);

                if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
                    return true;

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

        #endregion

        #region Request Models

        public class EditDocumentDetailsRequest
        {
            public int DocumentId { get; set; }
            public DateTime? DocumentDate { get; set; }
            public string DocumentNumber { get; set; }
            public string DocumentDescription { get; set; }
        }

        #endregion
    }
}
