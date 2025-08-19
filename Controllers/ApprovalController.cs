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
    public class ApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfGeneratorService _pdfGenerator;
        private readonly IWebHostEnvironment _environment;
        private readonly IApprovalService _approvalService;

        public ApprovalController(
            ApplicationDbContext context,
            IPdfGeneratorService pdfGenerator,
            IWebHostEnvironment environment,
            IApprovalService approvalService)
        {
            _context = context;
            _pdfGenerator = pdfGenerator;
            _environment = environment;
            _approvalService = approvalService;
        }

        [HttpGet]
        public async Task<IActionResult> Approve(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole == "User")
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk melakukan approval";
                return RedirectToAction("Index", "Permit");
            }

            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .Include(p => p.Documents)
                    .ThenInclude(d => d.UploadedByUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return RedirectToAction("Index", "Permit");
            }

            if (!CanUserApprove(userRole, permit.Status))
            {
                TempData["ErrorMessage"] = "Permohonan tidak dapat diproses pada tahap ini";
                return RedirectToAction("Detail", "Permit", new { id });
            }

            var model = new PermitApprovalViewModel
            {
                Id = permit.Id,
                ApplicationNumber = permit.ApplicationNumber,
                CompanyName = permit.CompanyName,
                ApplicantName = permit.User.NamaLengkap,
                CurrentStatus = permit.Status,
                SubmissionDate = permit.SubmissionDate,
                OriginLocation = permit.OriginLocation,
                DestinationLocation = permit.DestinationLocation,
                LivestockDetails = permit.LivestockDetails.Select(d => new LivestockDetailViewModel
                {
                    LivestockType = d.LivestockType,
                    Quantity = d.Quantity,
                    Description = d.Description
                }).ToList(),
                Documents = permit.Documents.Select(d => new DocumentViewModel
                {
                    Id = d.Id,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    FilePath = d.FilePath,
                    FileSize = d.FileSize,
                    FileExtension = d.FileExtension,
                    UploadDate = d.UploadDate,
                    UploadedBy = d.UploadedByUser.NamaLengkap,
                    DocumentDate = d.DocumentDate,
                    DocumentNumber = d.DocumentNumber,
                    DocumentDescription = d.DocumentDescription
                }).OrderBy(d => d.DocumentType).ToList(),
                DocumentContent = await GetDocumentContent(permit)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(PermitApprovalViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole == "User")
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk melakukan approval";
                return RedirectToAction("Index", "Permit");
            }

            if (string.IsNullOrEmpty(model.Action) || (model.Action != "Approve" && model.Action != "Reject"))
            {
                ModelState.AddModelError("Action", "Pilih aksi yang akan dilakukan");
                return View(model);
            }

            try
            {
                var result = await _approvalService.ProcessApprovalAsync(
                    model.Id,
                    model.Action,
                    model.Comments,
                    userId.Value,
                    userRole);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Permohonan {result.PermitApplicationNumber} berhasil {result.ActionText.ToLower()}";
                    return RedirectToAction("Index", "Permit");
                }
                else
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Terjadi kesalahan saat memproses approval. Silakan coba lagi.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EnableEditMode(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Hanya Admin yang dapat mengedit data permohonan";
                return RedirectToAction("Approve", new { id });
            }

            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .Include(p => p.Documents)
                    .ThenInclude(d => d.UploadedByUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return RedirectToAction("Index", "Permit");
            }

            if (!CanUserApprove(userRole, permit.Status))
            {
                TempData["ErrorMessage"] = "Permohonan tidak dapat diedit pada tahap ini";
                return RedirectToAction("Approve", new { id });
            }

            var model = new PermitApprovalViewModel
            {
                Id = permit.Id,
                ApplicationNumber = permit.ApplicationNumber,
                CompanyName = permit.CompanyName,
                ApplicantName = permit.User.NamaLengkap,
                CurrentStatus = permit.Status,
                SubmissionDate = permit.SubmissionDate,
                OriginLocation = permit.OriginLocation,
                DestinationLocation = permit.DestinationLocation,
                EditableCompanyName = permit.CompanyName,
                EditableCompanyAddress = permit.CompanyAddress,
                EditableOriginLocation = permit.OriginLocation,
                EditableDestinationLocation = permit.DestinationLocation,
                EditableDeparturePort = permit.DeparturePort,
                EditableArrivalPort = permit.ArrivalPort,
                IsEditingData = true,
                EditableLivestockDetails = permit.LivestockDetails.Select((d, index) => new EditableLivestockDetailViewModel
                {
                    Id = d.Id,
                    Index = index,
                    LivestockType = d.LivestockType,
                    Quantity = d.Quantity,
                    Description = d.Description,
                    IsMarkedForDeletion = false,
                    IsNewEntry = false
                }).ToList(),
                LivestockDetails = permit.LivestockDetails.Select(d => new LivestockDetailViewModel
                {
                    LivestockType = d.LivestockType,
                    Quantity = d.Quantity,
                    Description = d.Description
                }).ToList(),
                Documents = permit.Documents.Select(d => new DocumentViewModel
                {
                    Id = d.Id,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    FilePath = d.FilePath,
                    FileSize = d.FileSize,
                    FileExtension = d.FileExtension,
                    UploadDate = d.UploadDate,
                    UploadedBy = d.UploadedByUser.NamaLengkap,
                    DocumentDate = d.DocumentDate,
                    DocumentNumber = d.DocumentNumber,
                    DocumentDescription = d.DocumentDescription
                }).OrderBy(d => d.DocumentType).ToList()
            };

            if (!model.EditableLivestockDetails.Any())
            {
                model.EditableLivestockDetails.Add(new EditableLivestockDetailViewModel
                {
                    Index = 0,
                    IsNewEntry = true
                });
            }

            return View("ApproveWithEdit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveWithEdits(PermitApprovalViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Hanya Admin yang dapat mengedit dan menyetujui permohonan";
                return RedirectToAction("Index", "Permit");
            }

            if (string.IsNullOrEmpty(model.Action) || (model.Action != "Approve" && model.Action != "Reject"))
            {
                ModelState.AddModelError("Action", "Pilih aksi yang akan dilakukan");
                return View("ApproveWithEdit", model);
            }

            try
            {
                var result = await _approvalService.ProcessApprovalWithEditsAsync(model.Id, model, userId.Value);

                if (result.Success)
                {
                    var successMessage = $"Permohonan {result.PermitApplicationNumber} berhasil {result.ActionText.ToLower()}";
                    if (result.ChangedFields.Any())
                    {
                        successMessage += $" dengan {result.ChangedFields.Count} perubahan data";
                    }

                    TempData["SuccessMessage"] = successMessage;
                    return RedirectToAction("Index", "Permit");
                }
                else
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    return View("ApproveWithEdit", model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Terjadi kesalahan saat memproses approval. Silakan coba lagi.");
                return View("ApproveWithEdit", model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkApprove([FromBody] BulkActionRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole == "User") return Forbid();

            try
            {
                var result = await _approvalService.ProcessBulkApprovalAsync(
                    request.PermitIds,
                    "Approve",
                    request.Comments,
                    userId.Value,
                    userRole);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    successCount = result.SuccessCount,
                    errors = result.FailedPermits.Select(f => $"{f.PermitNumber}: {f.ErrorMessage}").ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Terjadi kesalahan: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkReject([FromBody] BulkActionRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole == "User") return Forbid();

            try
            {
                var result = await _approvalService.ProcessBulkApprovalAsync(
                    request.PermitIds,
                    "Reject",
                    request.Comments,
                    userId.Value,
                    userRole);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    successCount = result.SuccessCount,
                    errors = result.FailedPermits.Select(f => $"{f.PermitNumber}: {f.ErrorMessage}").ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Terjadi kesalahan: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetApprovalTimeline(int permitId)
        {
            try
            {
                var timeline = await _approvalService.GetApprovalTimelineAsync(permitId);

                return Json(new
                {
                    success = true,
                    data = timeline.Select(t => new
                    {
                        date = t.Date.ToString("yyyy-MM-dd HH:mm"),
                        action = t.Action,
                        actor = t.Actor,
                        actorRole = t.ActorRole,
                        comments = t.Comments,
                        timeAgo = t.TimeAgo,
                        isApproval = t.IsApproval,
                        isRejection = t.IsRejection
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPermitProgress(int id)
        {
            try
            {
                var permit = await _context.PermitApplications.FindAsync(id);
                if (permit == null)
                {
                    return NotFound();
                }

                var steps = PermitStatusHelper.GetProgressSteps(permit.Status);
                var progress = new
                {
                    percentage = PermitStatusHelper.GetProgressPercentage(permit.Status),
                    text = PermitStatusHelper.GetProgressText(permit.Status),
                    steps = steps.Select(s => new
                    {
                        title = s.Title,
                        icon = s.Icon,
                        isCompleted = s.IsCompleted,
                        isCurrent = s.IsCurrent,
                        isRejected = s.IsRejected
                    })
                };

                return Json(progress);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveEditsOnly(PermitApprovalViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Hanya Admin yang dapat menyimpan edit";
                return RedirectToAction("Index", "Permit");
            }

            try
            {
                var permit = await _context.PermitApplications
                    .Include(p => p.LivestockDetails)
                    .FirstOrDefaultAsync(p => p.Id == model.Id);

                if (permit == null)
                {
                    TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                    return RedirectToAction("Index", "Permit");
                }

                var changedFields = new List<string>();

                // Apply basic field changes
                var basicChanges = await ApplyBasicFieldChanges(permit, model, new Dictionary<string, string>());
                changedFields.AddRange(basicChanges);

                // Apply livestock changes
                var livestockChanges = await ApplyLivestockChanges(permit, model);
                changedFields.AddRange(livestockChanges);

                await _context.SaveChangesAsync();

                // Add admin history for the edit
                await _context.PermitApprovalHistories.AddAsync(new PermitApprovalHistory
                {
                    PermitApplicationId = permit.Id,
                    UserId = userId.Value,
                    Action = "Edit Data",
                    Comments = $"Admin mengedit data permohonan. Perubahan: {string.Join(", ", changedFields)}",
                    ActionDate = DateTime.Now
                });

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Data permohonan berhasil disimpan dengan {changedFields.Count} perubahan";
                return RedirectToAction("EnableEditMode", new { id = model.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Terjadi kesalahan saat menyimpan perubahan. Silakan coba lagi.");
                return View("ApproveWithEdit", model);
            }
        }

        #region Private Methods

        private int? GetCurrentUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }

        private bool CanUserApprove(string userRole, PermitStatus status)
        {
            return _approvalService.CanUserApprove(userRole, status);
        }

        private async Task<string> GetDocumentContent(LivestockPermitApplication permit)
        {
            try
            {
                if (!string.IsNullOrEmpty(permit.GeneratedDocumentPath))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", permit.GeneratedDocumentPath.TrimStart('/'));

                    if (System.IO.File.Exists(filePath))
                    {
                        var content = await System.IO.File.ReadAllTextAsync(filePath);
                        return content;
                    }
                }

                var htmlBytes = await _pdfGenerator.GeneratePermitPdf(permit);
                return System.Text.Encoding.UTF8.GetString(htmlBytes);
            }
            catch (Exception)
            {
                return "<div class='alert alert-danger'><i class='fas fa-exclamation-triangle'></i> Gagal memuat konten dokumen.</div>";
            }
        }

        private async Task<List<string>> ApplyBasicFieldChanges(LivestockPermitApplication permit, PermitApprovalViewModel model, Dictionary<string, string> originalData)
        {
            var changedFields = new List<string>();

            originalData["CompanyName"] = permit.CompanyName;
            originalData["CompanyAddress"] = permit.CompanyAddress ?? "";
            originalData["OriginLocation"] = permit.OriginLocation ?? "";
            originalData["DestinationLocation"] = permit.DestinationLocation ?? "";
            originalData["DeparturePort"] = permit.DeparturePort ?? "";
            originalData["ArrivalPort"] = permit.ArrivalPort ?? "";

            if (!string.IsNullOrEmpty(model.EditableCompanyName) &&
                permit.CompanyName != model.EditableCompanyName.Trim())
            {
                permit.CompanyName = model.EditableCompanyName.Trim();
                changedFields.Add($"Nama Perusahaan: '{originalData["CompanyName"]}' → '{permit.CompanyName}'");
            }

            if (!string.IsNullOrEmpty(model.EditableCompanyAddress) &&
                permit.CompanyAddress != model.EditableCompanyAddress.Trim())
            {
                permit.CompanyAddress = model.EditableCompanyAddress.Trim();
                changedFields.Add($"Alamat Perusahaan: '{originalData["CompanyAddress"]}' → '{permit.CompanyAddress}'");
            }

            if (!string.IsNullOrEmpty(model.EditableOriginLocation) &&
                permit.OriginLocation != model.EditableOriginLocation.Trim())
            {
                permit.OriginLocation = model.EditableOriginLocation.Trim();
                changedFields.Add($"Lokasi Asal: '{originalData["OriginLocation"]}' → '{permit.OriginLocation}'");
            }

            if (!string.IsNullOrEmpty(model.EditableDestinationLocation) &&
                permit.DestinationLocation != model.EditableDestinationLocation.Trim())
            {
                permit.DestinationLocation = model.EditableDestinationLocation.Trim();
                changedFields.Add($"Lokasi Tujuan: '{originalData["DestinationLocation"]}' → '{permit.DestinationLocation}'");
            }

            if (!string.IsNullOrEmpty(model.EditableDeparturePort) &&
                permit.DeparturePort != model.EditableDeparturePort.Trim())
            {
                permit.DeparturePort = model.EditableDeparturePort.Trim();
                changedFields.Add($"Pelabuhan Keberangkatan: '{originalData["DeparturePort"]}' → '{permit.DeparturePort}'");
            }

            if (!string.IsNullOrEmpty(model.EditableArrivalPort) &&
                permit.ArrivalPort != model.EditableArrivalPort.Trim())
            {
                permit.ArrivalPort = model.EditableArrivalPort.Trim();
                changedFields.Add($"Pelabuhan Tiba: '{originalData["ArrivalPort"]}' → '{permit.ArrivalPort}'");
            }

            return changedFields;
        }

        private async Task<List<string>> ApplyLivestockChanges(LivestockPermitApplication permit, PermitApprovalViewModel model)
        {
            var changedFields = new List<string>();

            if (model.EditableLivestockDetails == null || !model.EditableLivestockDetails.Any())
            {
                return changedFields;
            }

            var currentLivestock = permit.LivestockDetails.ToList();
            var originalCount = currentLivestock.Count;
            var originalSummary = string.Join(", ", currentLivestock.Select(l => $"{l.LivestockType}: {l.Quantity} ekor"));

            _context.LivestockDetails.RemoveRange(currentLivestock);

            var validLivestockDetails = model.EditableLivestockDetails
                .Where(d => !d.IsMarkedForDeletion &&
                           !string.IsNullOrEmpty(d.LivestockType) &&
                           d.Quantity > 0)
                .ToList();

            foreach (var editableLivestock in validLivestockDetails)
            {
                permit.LivestockDetails.Add(new LivestockDetail
                {
                    LivestockType = editableLivestock.LivestockType.Trim(),
                    Quantity = editableLivestock.Quantity,
                    Description = editableLivestock.Description?.Trim()
                });
            }

            var newCount = validLivestockDetails.Count;
            var newSummary = string.Join(", ", validLivestockDetails.Select(l => $"{l.LivestockType}: {l.Quantity} ekor"));
            var totalOriginal = currentLivestock.Sum(l => l.Quantity);
            var totalNew = validLivestockDetails.Sum(l => l.Quantity);

            if (newCount != originalCount || newSummary != originalSummary)
            {
                changedFields.Add($"Detail Ternak: '{originalSummary}' → '{newSummary}'");
                changedFields.Add($"Total Ternak: {totalOriginal} ekor → {totalNew} ekor");
            }

            return changedFields;
        }

        #endregion

        #region Request Models

        public class BulkActionRequest
        {
            public List<int> PermitIds { get; set; } = new();
            public string Comments { get; set; } = "";
        }

        #endregion
    }
}
