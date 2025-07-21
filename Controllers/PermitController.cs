using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.Services;
using PerizinanPeternakan.ViewModels;
using System.Text;

namespace PerizinanPeternakan.Controllers
{
    public class PermitController : Controller
    {
        private readonly IPermitService _permitService;
        private readonly IDocumentService _documentService;
        private readonly IAdminService _adminService;
        private readonly IWebHostEnvironment _environment;

        public PermitController(
            IPermitService permitService,
            IDocumentService documentService,
            IAdminService adminService,
            IWebHostEnvironment environment)
        {
            _permitService = permitService;
            _documentService = documentService;
            _adminService = adminService;
            _environment = environment;
        }

        #region Admin History Management

        public async Task<IActionResult> AdminHistory(
            DateTime? startDate = null,
            DateTime? endDate = null,
            PermitStatus? statusFilter = null,
            string searchTerm = null,
            int page = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Akses ditolak. Halaman ini hanya untuk Admin.";
                return RedirectToAction("Index", "Dashboard");
            }

            try
            {
                var (history, stats, pagination) = await _adminService.GetAdminHistoryAsync(
                    userId.Value, startDate, endDate, statusFilter, searchTerm, page);

                ViewBag.AdminName = stats.AdminName;
                ViewBag.TotalReviewed = stats.TotalReviewed;
                ViewBag.TotalApproved = stats.TotalApproved;
                ViewBag.TotalRejected = stats.TotalRejected;
                ViewBag.FilteredCount = stats.FilteredCount;

                ViewBag.CurrentPage = pagination.CurrentPage;
                ViewBag.TotalPages = pagination.TotalPages;
                ViewBag.HasPreviousPage = pagination.HasPreviousPage;
                ViewBag.HasNextPage = pagination.HasNextPage;
                ViewBag.StartDate = pagination.StartDate;
                ViewBag.EndDate = pagination.EndDate;
                ViewBag.StatusFilter = pagination.StatusFilter;
                ViewBag.SearchTerm = pagination.SearchTerm;

                return View(history);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat riwayat review.";
                return View(new List<AdminHistoryViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestAddAdminHistory()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Akses ditolak.";
                return RedirectToAction("Index", "Dashboard");
            }

            var availablePermits = await _adminService.GetAvailablePermitsForTestAsync();
            ViewBag.AvailablePermits = availablePermits.Select(p => new SelectListItem
            {
                Value = ((dynamic)p).Value,
                Text = ((dynamic)p).Text
            }).ToList();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAdminHistoryChart(int months = 6)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin") return Forbid();

            try
            {
                var monthlyData = await _adminService.GetAdminHistoryChartAsync(userId.Value, months);
                return Json(new { success = true, data = monthlyData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetAdminActivitySummary()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin") return Forbid();

            try
            {
                var summary = await _adminService.GetAdminActivitySummaryAsync(userId.Value);
                return Json(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddAdminHistory(int permitId, string action, string comments)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin")
            {
                return Json(new { success = false, message = "Akses ditolak" });
            }

            var result = await _adminService.AddAdminHistoryAsync(permitId, action, comments, userId.Value);
            return Json(new { success = result.Success, message = result.Message });
        }

        public async Task<IActionResult> AdminStatistics()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin") return Forbid();

            try
            {
                var stats = await _adminService.GetAdminStatisticsAsync(userId.Value);
                return Json(stats);
            }
            catch (Exception)
            {
                return Json(new { error = "Failed to load statistics" });
            }
        }

        #endregion

        #region Permit CRUD Operations

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            var permits = await _permitService.GetPermitsForUserAsync(userRole!, userId.Value);

            return View(permits);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Auth");
            if (HttpContext.Session.GetString("Role") != "User")
            {
                TempData["ErrorMessage"] = "Hanya user yang dapat membuat permohonan";
                return RedirectToAction("Index");
            }

            var model = new PermitApplicationViewModel();
            model.LivestockDetails.Add(new LivestockDetailViewModel());
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PermitApplicationViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");
            if (HttpContext.Session.GetString("Role") != "User")
            {
                TempData["ErrorMessage"] = "Hanya user yang dapat membuat permohonan";
                return RedirectToAction("Index");
            }

            try
            {
                var result = await _permitService.CreatePermitAsync(model, userId.Value);

                if (!result.Success)
                {
                    ModelState.AddModelError("", result.ErrorMessage);

                    if (model.LivestockDetails == null || !model.LivestockDetails.Any())
                    {
                        model.LivestockDetails.Add(new LivestockDetailViewModel());
                    }

                    return View(model);
                }

                TempData["SuccessMessage"] = $"Permohonan izin berhasil diajukan dengan nomor {result.ApplicationNumber}. " +
                                           "Permohonan Anda akan segera diproses oleh tim admin.";

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Terjadi kesalahan saat menyimpan permohonan. Silakan coba lagi.");

                if (model.LivestockDetails == null || !model.LivestockDetails.Any())
                {
                    model.LivestockDetails.Add(new LivestockDetailViewModel());
                }

                return View(model);
            }
        }

        public async Task<IActionResult> Detail(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            var permit = await _permitService.GetPermitDetailAsync(id, userRole!, userId.Value);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan atau Anda tidak memiliki akses";
                return RedirectToAction("Index");
            }

            return View(permit);
        }

        #endregion

        #region Approval Operations

        [HttpGet]
        public async Task<IActionResult> Approve(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole == "User")
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk melakukan approval";
                return RedirectToAction("Index");
            }

            var permit = await _permitService.GetPermitDetailAsync(id, userRole!, userId.Value);
            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return RedirectToAction("Index");
            }

            if (!_permitService.CanUserApprove(userRole!, permit.Status))
            {
                TempData["ErrorMessage"] = "Permohonan tidak dapat diproses pada tahap ini";
                return RedirectToAction("Detail", new { id });
            }

            var model = new PermitApprovalViewModel
            {
                Id = permit.Id,
                ApplicationNumber = permit.ApplicationNumber,
                CompanyName = permit.CompanyName,
                ApplicantName = permit.ApplicantName,
                CurrentStatus = permit.Status,
                SubmissionDate = permit.SubmissionDate,
                OriginLocation = permit.OriginLocation,
                DestinationLocation = permit.DestinationLocation,
                LivestockDetails = permit.LivestockDetails,
                Documents = permit.Documents
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
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(model.Action) || (model.Action != "Approve" && model.Action != "Reject"))
            {
                ModelState.AddModelError("Action", "Pilih aksi yang akan dilakukan");
                return View(model);
            }

            try
            {
                var result = await _permitService.ApprovePermitAsync(
                    model.Id, model.Action, model.Comments, userId.Value, userRole!);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Approve", new { id = model.Id });
                }

                var permit = await _permitService.GetPermitDetailAsync(model.Id, userRole!, userId.Value);
                TempData["SuccessMessage"] = $"Permohonan {permit?.ApplicationNumber} berhasil {model.Action.ToLower()}";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Terjadi kesalahan saat memproses approval. Silakan coba lagi.");
                return View(model);
            }
        }

        #endregion

        #region Document Management

        public async Task<IActionResult> DownloadDocument(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            var result = await _documentService.GetDocumentForDownloadAsync(id, userRole!, userId.Value);

            if (!result.CanAccess)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengunduh dokumen ini";
                return Forbid();
            }

            if (result.FileBytes == null)
            {
                TempData["ErrorMessage"] = "File tidak ditemukan di server";
                return NotFound();
            }

            return File(result.FileBytes, result.ContentType!, result.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> ViewDocument(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            var permit = await _permitService.GetPermitDetailAsync(id, userRole!, userId.Value);

            if (permit == null)
            {
                return NotFound();
            }

            // Generate document if needed
            if (string.IsNullOrEmpty(permit.GeneratedDocumentPath))
            {
                await _permitService.GeneratePermitDocumentAsync(permit.Id);
            }

            var viewModel = new PermitDocumentViewModel
            {
                PermitId = permit.Id,
                ApplicationNumber = permit.ApplicationNumber,
                CompanyName = permit.CompanyName,
                Status = permit.Status,
                DocumentContent = await _documentService.GetDocumentContentAsync(
                    new LivestockPermitApplication { Id = permit.Id, GeneratedDocumentPath = permit.GeneratedDocumentPath }),
                CanApprove = _permitService.CanUserApprove(userRole!, permit.Status),
                UserRole = userRole!
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetDocumentFile(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");
            var fileBytes = await _documentService.GetDocumentFileAsync(id, userRole!, userId.Value);

            if (fileBytes.Length == 0)
            {
                return NotFound();
            }

            return File(fileBytes, "text/html");
        }

        public async Task<IActionResult> Download(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");

            // User hanya bisa download jika role adalah User
            if (userRole != "User")
            {
                TempData["ErrorMessage"] = "Download hanya tersedia untuk pemohon";
                return RedirectToAction("Detail", new { id });
            }

            var permit = await _permitService.GetPermitDetailAsync(id, userRole, userId.Value);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return RedirectToAction("Index");
            }

            if (permit.Status != PermitStatus.FinalApproved || string.IsNullOrEmpty(permit.GeneratedDocumentPath))
            {
                TempData["ErrorMessage"] = "Dokumen belum tersedia untuk didownload. Menunggu persetujuan akhir.";
                return RedirectToAction("Detail", new { id });
            }

            try
            {
                var fileBytes = await _documentService.GetDocumentFileAsync(id, userRole, userId.Value);

                if (fileBytes.Length == 0)
                {
                    TempData["ErrorMessage"] = "File dokumen tidak ditemukan. Sedang memproses ulang...";
                    return RedirectToAction("Detail", new { id });
                }

                var fileName = $"Izin_Pengeluaran_Ternak_{permit.ApplicationNumber.Replace("/", "_")}.html";
                return File(fileBytes, "text/html", fileName);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mengunduh dokumen";
                return RedirectToAction("Detail", new { id });
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
                return RedirectToAction("Index");
            }

            var document = await _documentService.GetDocumentAsync(documentId, userRole, userId.Value);

            if (document == null)
            {
                TempData["ErrorMessage"] = "Dokumen tidak ditemukan";
                return RedirectToAction("Index");
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

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDocumentDetails(DocumentDetailsViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin" && userRole != "Verifikator")
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengedit detail dokumen";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _documentService.UpdateDocumentDetailsAsync(model.DocumentId, model, userId.Value);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage);
                return View(model);
            }

            TempData["SuccessMessage"] = "Detail dokumen berhasil diperbarui";
            // Redirect to permit detail - need to get permit ID from document
            var document = await _documentService.GetDocumentAsync(model.DocumentId, userRole, userId.Value);
            return RedirectToAction("Detail", new { id = document?.Id ?? 0 });
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

            var result = await _documentService.BulkUpdateDocumentDetailsAsync(model.DocumentDetails, userId.Value);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                updatedCount = result.UpdatedCount
            });
        }

        [HttpPost]
        public async Task<IActionResult> ValidateDocumentDetailsAPI([FromBody] List<DocumentDetailsViewModel> documentDetails)
        {
            try
            {
                var validationResults = await _documentService.ValidateDocumentDetailsAPIAsync(documentDetails);

                return Json(new
                {
                    success = true,
                    results = validationResults,
                    allValid = validationResults.All(r => (bool)((dynamic)r).isValid)
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

        #endregion

        #region API Endpoints for DataTables and AJAX

        [HttpGet]
        public async Task<IActionResult> GetPermitsData(
            int draw = 1,
            int start = 0,
            int length = 10,
            string search = "",
            string statusFilter = "",
            string dateFilter = "")
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var (data, totalCount) = await _permitService.GetPermitsDataAsync(
                    userRole!, userId.Value, start, length, search, statusFilter, dateFilter);

                return Json(new
                {
                    draw = draw,
                    recordsTotal = totalCount,
                    recordsFiltered = totalCount,
                    data = data
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPermitStatistics()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var stats = await _permitService.GetPermitStatisticsAsync(userRole!, userId.Value);
                return Json(stats);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToCsv(
            string statusFilter = "",
            string dateFrom = "",
            string dateTo = "",
            string search = "")
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var csvBytes = await _permitService.ExportToCsvAsync(
                    userRole!, userId.Value, statusFilter, dateFrom, dateTo, search);

                var fileName = $"daftar_permohonan_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(csvBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gagal mengekspor data: {ex.Message}";
                return RedirectToAction("Index");
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
                var result = await _permitService.BulkApproveAsync(
                    request.PermitIds, request.Comments, userId.Value, userRole!);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    successCount = result.SuccessCount,
                    errors = result.Errors
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
                var result = await _permitService.BulkRejectAsync(
                    request.PermitIds, request.Comments, userId.Value, userRole!);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    successCount = result.SuccessCount,
                    errors = result.Errors
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
        public async Task<IActionResult> GetPermitProgress(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Unauthorized();

                var userRole = HttpContext.Session.GetString("Role");
                var permit = await _permitService.GetPermitDetailAsync(id, userRole!, userId.Value);

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
        public async Task<IActionResult> AdvancedSearch([FromBody] AdvancedSearchRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var results = await _permitService.AdvancedSearchAsync(request, userRole!, userId.Value);

                return Json(new
                {
                    success = true,
                    data = results,
                    count = results.Count
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
        public async Task<IActionResult> GetDashboardData()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var dashboardData = await _permitService.GetDashboardDataAsync(userRole!, userId.Value);
                return Json(dashboardData);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        #endregion

        #region Development/Debug Methods

        [HttpGet]
        public async Task<IActionResult> TestUpload()
        {
            if (_environment.IsDevelopment())
            {
                var userId = GetCurrentUserId();
                if (userId == null) return RedirectToAction("Login", "Auth");

                var (results, success) = _adminService.TestUploadDirectories(_environment);
                ViewBag.TestResults = results;
                return View();
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> CheckDocuments(int? permitId)
        {
            if (_environment.IsDevelopment())
            {
                var results = await _adminService.CheckDocumentsAsync(_environment, permitId);
                ViewBag.DocumentResults = results;
                return View("TestUpload");
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> CleanupOrphanedFiles()
        {
            if (_environment.IsDevelopment())
            {
                var userId = GetCurrentUserId();
                if (userId == null) return RedirectToAction("Login", "Auth");

                try
                {
                    var (success, results) = await _adminService.CleanupOrphanedFilesAsync(_environment);

                    if (success)
                    {
                        TempData["SuccessMessage"] = $"Cleanup completed. {results.Count} operations performed.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = string.Join("; ", results);
                    }

                    ViewBag.CleanupResults = results;
                    return View("TestUpload");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Cleanup failed: {ex.Message}";
                    return View("TestUpload");
                }
            }

            return NotFound();
        }

        #endregion

        #region Helper Methods

        private int? GetCurrentUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }

        #endregion

        #region Request Models for API

        public class BulkActionRequest
        {
            public List<int> PermitIds { get; set; } = new();
            public string Comments { get; set; } = "";
        }

        #endregion

        
        #region Additional Dashboard API Endpoints

        [HttpGet]
        public async Task<IActionResult> GetPermitStatusDistribution()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var stats = await _permitService.GetPermitStatisticsAsync(userRole!, userId.Value);
                var statsObj = (dynamic)stats;

                return Json(new
                {
                    success = true,
                    data = statsObj.statusDistribution
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyTrend(int months = 6)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var trend = await _permitService.GetMonthlyTrendAsync(userRole!, userId.Value, months);
                return Json(new
                {
                    success = true,
                    data = trend
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var dashboardData = await _permitService.GetDashboardDataAsync(userRole!, userId.Value);
                var data = (dynamic)dashboardData;

                return Json(new
                {
                    success = true,
                    summary = data.summary,
                    performance = data.performance,
                    quickActions = data.quickActions
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivity(int count = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var dashboardData = await _permitService.GetDashboardDataAsync(userRole!, userId.Value);
                var data = (dynamic)dashboardData;

                return Json(new
                {
                    success = true,
                    data = data.recentActivity
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUrgentItems()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var dashboardData = await _permitService.GetDashboardDataAsync(userRole!, userId.Value);
                var data = (dynamic)dashboardData;

                return Json(new
                {
                    success = true,
                    data = data.urgentItems
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkloadAnalysis()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var stats = await _permitService.GetPermitStatisticsAsync(userRole!, userId.Value);
                var dashboardData = await _permitService.GetDashboardDataAsync(userRole!, userId.Value);

                var statsObj = (dynamic)stats;
                var dashboardObj = (dynamic)dashboardData;

                return Json(new
                {
                    success = true,
                    workload = new
                    {
                        totalAssigned = statsObj.total,
                        pendingReview = statsObj.pending,
                        completedToday = statsObj.today,
                        completedThisWeek = statsObj.thisWeek,
                        completedThisMonth = statsObj.thisMonth,
                        averageProcessingDays = statsObj.avgProcessingDays,
                        completionRate = dashboardObj.performance.completionRate,
                        rejectionRate = dashboardObj.performance.rejectionRate
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPerformanceMetrics()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole == "User") return Forbid(); // Only for staff roles

            try
            {
                var dashboardData = await _permitService.GetDashboardDataAsync(userRole!, userId.Value);
                var data = (dynamic)dashboardData;

                return Json(new
                {
                    success = true,
                    metrics = data.performance,
                    quickActions = data.quickActions
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        #endregion
    }
}