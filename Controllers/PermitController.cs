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
    public class PermitController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfGeneratorService _pdfGenerator;
        private readonly IWebHostEnvironment _environment;
        private readonly IApplicationNumberService _applicationNumberService;
        private readonly IDocumentService _documentService;
        private readonly IApprovalService _approvalService;

        public PermitController (
            ApplicationDbContext context, 
            IPdfGeneratorService pdfGenerator, 
            IWebHostEnvironment environment, 
            IApplicationNumberService applicationNumberService, 
            IDocumentService documentService,
            IApprovalService approvalService
            )
        {
            _context = context;
            _pdfGenerator = pdfGenerator;
            _environment = environment;
            _applicationNumberService = applicationNumberService;
            _documentService = documentService;
            _approvalService = approvalService;
        }

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
                int pageSize = 10;

                var query = _context.PermitApplications
                    .Where(p => p.AdminId == userId.Value &&
                               (p.Status >= PermitStatus.AdminApproved || p.Status == PermitStatus.AdminRejected))
                    .Include(p => p.User)
                    .Include(p => p.ApprovalHistory.Where(h => h.UserId == userId.Value))
                    .Include(p => p.Documents)
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(p => p.AdminApprovalDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(p => p.AdminApprovalDate <= endDate.Value);
                }

                if (statusFilter.HasValue)
                {
                    query = query.Where(p => p.Status == statusFilter.Value);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(p =>
                        p.ApplicationNumber.Contains(searchTerm) ||
                        p.User.NamaLengkap.Contains(searchTerm) ||
                        p.CompanyName.Contains(searchTerm));
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var adminHistoryList = await query
                    .OrderByDescending(p => p.AdminApprovalDate ?? p.SubmissionDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new AdminHistoryViewModel
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = p.Status,
                        SubmissionDate = p.SubmissionDate,
                        AdminApprovalDate = p.AdminApprovalDate,
                        AdminComments = p.ApprovalHistory
                            .Where(h => h.UserId == userId.Value && h.Action.Contains("Admin"))
                            .OrderByDescending(h => h.ActionDate)
                            .Select(h => h.Comments)
                            .FirstOrDefault(),
                        AdminAction = p.ApprovalHistory
                            .Where(h => h.UserId == userId.Value && h.Action.Contains("Admin"))
                            .OrderByDescending(h => h.ActionDate)
                            .Select(h => h.Action)
                            .FirstOrDefault(),
                        DocumentCount = p.Documents.Count,
                        CanView = true
                    })
                    .ToListAsync();

                var allAdminHistory = await _context.PermitApplications
                    .Where(p => p.AdminId == userId.Value)
                    .ToListAsync();

                ViewBag.AdminName = HttpContext.Session.GetString("NamaLengkap");
                ViewBag.TotalReviewed = allAdminHistory.Count;
                ViewBag.TotalApproved = allAdminHistory.Count(h =>
                    h.Status == PermitStatus.AdminApproved ||
                    h.Status == PermitStatus.VerifikatorApproved ||
                    h.Status == PermitStatus.FinalApproved);
                ViewBag.TotalRejected = allAdminHistory.Count(h => h.Status == PermitStatus.AdminRejected);
                ViewBag.FilteredCount = totalItems;

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < totalPages;

                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
                ViewBag.StatusFilter = statusFilter;
                ViewBag.SearchTerm = searchTerm;

                return View(adminHistoryList);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat riwayat review.";
                return View(new List<AdminHistoryViewModel>());
            }
        }

        public async Task<IActionResult> VerifikatorHistory(
            DateTime? startDate = null,
            DateTime? endDate = null,
            PermitStatus? statusFilter = null,
            string searchTerm = null,
            int page = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Verifikator")
            {
                TempData["ErrorMessage"] = "Akses ditolak. Halaman ini hanya untuk Verifikator.";
                return RedirectToAction("Index", "Dashboard");
            }

            try
            {
                int pageSize = 10;

                var query = _context.PermitApplications
                    .Where(p => p.VerifikatorId == userId.Value &&
                               (p.Status >= PermitStatus.VerifikatorApproved || p.Status == PermitStatus.VerifikatorRejected))
                    .Include(p => p.User)
                    .Include(p => p.ApprovalHistory.Where(h => h.UserId == userId.Value))
                    .Include(p => p.Documents)
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(p => p.VerificationDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(p => p.VerificationDate <= endDate.Value);
                }

                if (statusFilter.HasValue)
                {
                    query = query.Where(p => p.Status == statusFilter.Value);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(p =>
                        p.ApplicationNumber.Contains(searchTerm) ||
                        p.User.NamaLengkap.Contains(searchTerm) ||
                        p.CompanyName.Contains(searchTerm));
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var verifikatorHistoryList = await query
                    .OrderByDescending(p => p.VerificationDate ?? p.SubmissionDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new VerifikatorHistoryViewModel
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = p.Status,
                        SubmissionDate = p.SubmissionDate,
                        VerificationDate = p.VerificationDate,
                        VerifikatorComments = p.ApprovalHistory
                            .Where(h => h.UserId == userId.Value && h.Action.Contains("Verifikator"))
                            .OrderByDescending(h => h.ActionDate)
                            .Select(h => h.Comments)
                            .FirstOrDefault(),
                        VerifikatorAction = p.ApprovalHistory
                            .Where(h => h.UserId == userId.Value && h.Action.Contains("Verifikator"))
                            .OrderByDescending(h => h.ActionDate)
                            .Select(h => h.Action)
                            .FirstOrDefault(),
                        DocumentCount = p.Documents.Count,
                        CanView = true
                    })
                    .ToListAsync();

                var allVerifikatorHistory = await _context.PermitApplications
                    .Where(p => p.VerifikatorId == userId.Value)
                    .ToListAsync();

                ViewBag.VerifikatorName = HttpContext.Session.GetString("NamaLengkap");
                ViewBag.TotalReviewed = allVerifikatorHistory.Count;
                ViewBag.TotalApproved = allVerifikatorHistory.Count(h =>
                    h.Status == PermitStatus.VerifikatorApproved ||
                    h.Status == PermitStatus.FinalApproved);
                ViewBag.TotalRejected = allVerifikatorHistory.Count(h => h.Status == PermitStatus.VerifikatorRejected);
                ViewBag.FilteredCount = totalItems;

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < totalPages;

                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
                ViewBag.StatusFilter = statusFilter;
                ViewBag.SearchTerm = searchTerm;

                return View(verifikatorHistoryList);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat riwayat verifikasi.";
                return View(new List<VerifikatorHistoryViewModel>());
            }
        }

        public async Task<IActionResult> KepalaDinasHistory(
            DateTime? startDate = null,
            DateTime? endDate = null,
            PermitStatus? statusFilter = null,
            string searchTerm = null,
            int page = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "KepalaDinas")
            {
                TempData["ErrorMessage"] = "Akses ditolak. Halaman ini hanya untuk Kepala Dinas.";
                return RedirectToAction("Index", "Dashboard");
            }

            try
            {
                int pageSize = 10;

                var query = _context.PermitApplications
                    .Where(p => p.KepalaDinasId == userId.Value &&
                               (p.Status >= PermitStatus.FinalApproved || p.Status == PermitStatus.FinalRejected))
                    .Include(p => p.User)
                    .Include(p => p.ApprovalHistory.Where(h => h.UserId == userId.Value))
                    .Include(p => p.Documents)
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(p => p.FinalApprovalDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(p => p.FinalApprovalDate <= endDate.Value);
                }

                if (statusFilter.HasValue)
                {
                    query = query.Where(p => p.Status == statusFilter.Value);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(p =>
                        p.ApplicationNumber.Contains(searchTerm) ||
                        p.User.NamaLengkap.Contains(searchTerm) ||
                        p.CompanyName.Contains(searchTerm));
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var kepalaDinasHistoryList = await query
                    .OrderByDescending(p => p.FinalApprovalDate ?? p.SubmissionDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new KepalaDinasHistoryViewModel
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = p.Status,
                        SubmissionDate = p.SubmissionDate,
                        FinalApprovalDate = p.FinalApprovalDate,
                        KepalaDinasComments = p.ApprovalHistory
                            .Where(h => h.UserId == userId.Value && h.Action.Contains("KepalaDinas"))
                            .OrderByDescending(h => h.ActionDate)
                            .Select(h => h.Comments)
                            .FirstOrDefault(),
                        KepalaDinasAction = p.ApprovalHistory
                            .Where(h => h.UserId == userId.Value && h.Action.Contains("KepalaDinas"))
                            .OrderByDescending(h => h.ActionDate)
                            .Select(h => h.Action)
                            .FirstOrDefault(),
                        DocumentCount = p.Documents.Count,
                        CanView = true
                    })
                    .ToListAsync();

                var allKepalaDinasHistory = await _context.PermitApplications
                    .Where(p => p.KepalaDinasId == userId.Value)
                    .ToListAsync();

                ViewBag.KepalaDinasName = HttpContext.Session.GetString("NamaLengkap");
                ViewBag.TotalReviewed = allKepalaDinasHistory.Count;
                ViewBag.TotalApproved = allKepalaDinasHistory.Count(h =>
                    h.Status == PermitStatus.FinalApproved);
                ViewBag.TotalRejected = allKepalaDinasHistory.Count(h => h.Status == PermitStatus.FinalRejected);
                ViewBag.FilteredCount = totalItems;

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < totalPages;

                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
                ViewBag.StatusFilter = statusFilter;
                ViewBag.SearchTerm = searchTerm;

                return View(kepalaDinasHistoryList);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat riwayat persetujuan.";
                return View(new List<KepalaDinasHistoryViewModel>());
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetCurrentUserData()
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");

                if (string.IsNullOrEmpty(username))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Session tidak valid atau sudah berakhir. Silakan login kembali.",
                        redirectToLogin = true
                    });
                }

                var user = await _context.Users
                    .Where(u => u.Username == username)
                    .Select(u => new {
                        namaLengkap = u.NamaLengkap,
                        alamat = u.Alamat,
                        email = u.Email,
                        noTelepon = u.NoTelepon,
                        username = u.Username,
                        userId = u.Id
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Data user tidak ditemukan"
                    });
                }

                var response = new
                {
                    success = true,
                    data = new
                    {
                        namaLengkap = user.namaLengkap ?? "",
                        alamat = user.alamat ?? "",
                        email = user.email ?? "",
                        noTelepon = user.noTelepon ?? "",
                        username = user.username ?? "",

                        hasCompleteProfile = !string.IsNullOrEmpty(user.namaLengkap) &&
                                           !string.IsNullOrEmpty(user.alamat),
                        displayName = !string.IsNullOrEmpty(user.namaLengkap) ? user.namaLengkap : user.username,

                        dataFetchedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        profileCompleteness = CalculateProfileCompleteness(user),

                        sessionInfo = new
                        {
                            role = HttpContext.Session.GetString("Role") ?? "",
                            namaLengkapSession = HttpContext.Session.GetString("NamaLengkap") ?? "",
                            isAuthenticated = true
                        }
                    },
                    message = "Data user berhasil dimuat"
                };

                return Json(response);
            }
            catch (Exception ex)
            {

                return Json(new
                {
                    success = false,
                    message = "Terjadi kesalahan saat mengambil data user",
                    error = ex.Message // Remove this in production
                });
            }
        }

        private int CalculateProfileCompleteness(dynamic user)
        {
            try
            {
                int totalFields = 4; 
                int completedFields = 0;

                if (!string.IsNullOrEmpty(user.namaLengkap)) completedFields++;
                if (!string.IsNullOrEmpty(user.alamat)) completedFields++;
                if (!string.IsNullOrEmpty(user.email)) completedFields++;
                if (!string.IsNullOrEmpty(user.noTelepon)) completedFields++;

                return (int)Math.Round((double)completedFields / totalFields * 100);
            }
            catch
            {
                return 0;
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

            var availablePermits = await _context.PermitApplications
                .Where(p => p.Status == PermitStatus.Submitted)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.ApplicationNumber} - {p.CompanyName}"
                })
                .ToListAsync();

            ViewBag.AvailablePermits = availablePermits;
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
                var startDate = DateTime.Now.AddMonths(-months);

                var monthlyData = await _context.PermitApprovalHistories
                    .Where(h => h.UserId == userId.Value &&
                               h.Action.Contains("Admin") &&
                               h.ActionDate >= startDate)
                    .GroupBy(h => new {
                        Year = h.ActionDate.Year,
                        Month = h.ActionDate.Month
                    })
                    .Select(g => new
                    {
                        year = g.Key.Year,
                        month = g.Key.Month,
                        monthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                        totalReviewed = g.Count(),
                        totalApproved = g.Count(h => h.Action.Contains("Disetujui")),
                        totalRejected = g.Count(h => h.Action.Contains("Ditolak"))
                    })
                    .OrderBy(x => x.year).ThenBy(x => x.month)
                    .ToListAsync();

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
                var today = DateTime.Today;
                var thisWeek = today.AddDays(-7);
                var thisMonth = new DateTime(today.Year, today.Month, 1);

                var summary = new
                {
                    todayReviews = await _context.PermitApprovalHistories
                        .CountAsync(h => h.UserId == userId.Value &&
                                       h.Action.Contains("Admin") &&
                                       h.ActionDate.Date == today),

                    weekReviews = await _context.PermitApprovalHistories
                        .CountAsync(h => h.UserId == userId.Value &&
                                       h.Action.Contains("Admin") &&
                                       h.ActionDate >= thisWeek),

                    monthReviews = await _context.PermitApprovalHistories
                        .CountAsync(h => h.UserId == userId.Value &&
                                       h.Action.Contains("Admin") &&
                                       h.ActionDate >= thisMonth),

                    pendingReviews = await _context.PermitApplications
                        .CountAsync(p => p.Status == PermitStatus.Submitted),

                    avgProcessingTime = await _context.PermitApplications
                        .Where(p => p.AdminId == userId.Value &&
                                   p.SubmissionDate != null &&
                                   p.AdminApprovalDate.HasValue)
                        .Select(p => EF.Functions.DateDiffDay(p.SubmissionDate, p.AdminApprovalDate.Value))
                        .AverageAsync()
                };

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

            try
            {
                var permit = await _context.PermitApplications.FindAsync(permitId);
                if (permit == null)
                {
                    return Json(new { success = false, message = "Permohonan tidak ditemukan" });
                }

                var fromStatus = permit.Status;
                var toStatus = action.Contains("Disetujui") ?
                    PermitStatus.AdminApproved : PermitStatus.AdminRejected;

                var history = new PermitApprovalHistory
                {
                    PermitApplicationId = permitId,
                    UserId = userId.Value,
                    FromStatus = fromStatus,
                    ToStatus = toStatus,
                    Action = action,
                    Comments = comments ?? $"Review admin untuk permohonan {permit.ApplicationNumber}",
                    ActionDate = DateTime.Now
                };

                _context.PermitApprovalHistories.Add(history);

                permit.Status = toStatus;
                permit.AdminId = userId.Value;
                permit.AdminApprovalDate = DateTime.Now;

                if (toStatus == PermitStatus.AdminRejected)
                {
                    permit.RejectionReason = comments;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"History admin berhasil ditambahkan untuk {permit.ApplicationNumber}"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat menambahkan history" });
            }
        }
        public async Task<IActionResult> AdminStatistics()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin") return Forbid();

            try
            {
                var stats = await _approvalService.GetApprovalStatisticsAsync(userId.Value, userRole);

                return Json(new
                {
                    totalReviewed = stats.TotalReviewed,
                    totalApproved = stats.TotalApproved,
                    totalRejected = stats.TotalRejected,
                    thisMonthReviewed = stats.ThisMonthReviewed,
                    averageProcessingDays = stats.AverageProcessingDays,
                    approvalRate = stats.ApprovalRate
                });
            }
            catch (Exception)
            {
                return Json(new { error = "Failed to load statistics" });
            }
        }
        // NEW - API endpoint untuk approval timeline:
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
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            var permits = new List<PermitListViewModel>();

            if (userRole == "User")
            {
                permits = await _context.PermitApplications
                    .Where(p => p.UserId == userId.Value)
                    .Include(p => p.Documents)
                    .Select(p => new PermitListViewModel
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = p.Status,
                        SubmissionDate = p.SubmissionDate,
                        AdminApprovalDate = p.AdminApprovalDate,
                        VerificationDate = p.VerificationDate,
                        FinalApprovalDate = p.FinalApprovalDate,
                        OriginLocation = p.OriginLocation,
                        DestinationLocation = p.DestinationLocation,
                        CanDownload = p.Status == PermitStatus.FinalApproved && !string.IsNullOrEmpty(p.GeneratedDocumentPath),
                        CanView = true,
                        CanApprove = false,
                        GeneratedDocumentPath = p.GeneratedDocumentPath,
                        CurrentApprovalLevel = p.CurrentApprovalLevel,
                        DocumentCount = p.Documents.Count,
                        HasAllRequiredDocuments = p.Documents.Count >= 7
                    })
                    .OrderByDescending(p => p.SubmissionDate)
                    .ToListAsync();
            }
            else if (userRole == "Admin")
            {
                permits = await _context.PermitApplications
                    .Where(p => p.Status == PermitStatus.Submitted || p.Status == PermitStatus.UnderAdminReview)
                    .Include(p => p.Documents)
                    .Select(p => new PermitListViewModel
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = p.Status,
                        SubmissionDate = p.SubmissionDate,
                        OriginLocation = p.OriginLocation,
                        DestinationLocation = p.DestinationLocation,
                        CanDownload = false,
                        CanView = true,
                        CanApprove = true,
                        CurrentApprovalLevel = p.CurrentApprovalLevel,
                        DocumentCount = p.Documents.Count,
                        HasAllRequiredDocuments = p.Documents.Count >= 7
                    })
                    .OrderByDescending(p => p.SubmissionDate)
                    .ToListAsync();
            }
            else if (userRole == "Verifikator")
            {
                permits = await _context.PermitApplications
                    .Where(p => p.Status == PermitStatus.AdminApproved || p.Status == PermitStatus.UnderVerifikatorReview)
                    .Include(p => p.Documents)
                    .Select(p => new PermitListViewModel
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = p.Status,
                        SubmissionDate = p.SubmissionDate,
                        AdminApprovalDate = p.AdminApprovalDate,
                        OriginLocation = p.OriginLocation,
                        DestinationLocation = p.DestinationLocation,
                        CanDownload = false,
                        CanView = true,
                        CanApprove = true,
                        GeneratedDocumentPath = p.GeneratedDocumentPath,
                        CurrentApprovalLevel = p.CurrentApprovalLevel,
                        DocumentCount = p.Documents.Count,
                        HasAllRequiredDocuments = p.Documents.Count >= 7
                    })
                    .OrderByDescending(p => p.AdminApprovalDate ?? p.SubmissionDate)
                    .ToListAsync();
            }
            else if (userRole == "KepalaDinas")
            {
                permits = await _context.PermitApplications
                    .Where(p => p.Status == PermitStatus.VerifikatorApproved || p.Status == PermitStatus.PendingKepalaDinas)
                    .Include(p => p.Documents)
                    .Select(p => new PermitListViewModel
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = p.Status,
                        SubmissionDate = p.SubmissionDate,
                        AdminApprovalDate = p.AdminApprovalDate,
                        VerificationDate = p.VerificationDate,
                        OriginLocation = p.OriginLocation,
                        DestinationLocation = p.DestinationLocation,
                        CanDownload = false,
                        CanView = true,
                        CanApprove = true,
                        GeneratedDocumentPath = p.GeneratedDocumentPath,
                        CurrentApprovalLevel = p.CurrentApprovalLevel,
                        DocumentCount = p.Documents.Count,
                        HasAllRequiredDocuments = p.Documents.Count >= 7
                    })
                    .OrderByDescending(p => p.VerificationDate ?? p.AdminApprovalDate ?? p.SubmissionDate)
                    .ToListAsync();
            }

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
            Console.WriteLine("🚀 Create action started");
            Console.WriteLine($"🔍 DEBUG - ApplicantType: '{model.ApplicantType}'");
            Console.WriteLine($"🔍 DEBUG - CompanyName: '{model.CompanyName}'");
            Console.WriteLine($"🔍 DEBUG - IndividualName: '{model.IndividualName}'");
            Console.WriteLine($"🔍 DEBUG - CompanyProvince: '{model.CompanyProvince}'");
            Console.WriteLine($"🔍 DEBUG - CompanyRegency: '{model.CompanyRegency}'");
            Console.WriteLine($"🔍 DEBUG - AddressStreet: '{model.AddressStreet}'");
            
            // Log all form data for debugging
            Console.WriteLine("🔍 DEBUG - All form data:");
            foreach (var property in typeof(PermitApplicationViewModel).GetProperties())
            {
                var value = property.GetValue(model);
                Console.WriteLine($"  {property.Name}: '{value}'");
            }
            
            // Check if ModelState already has errors for CompanyName/CompanyAddress
            Console.WriteLine("🔍 DEBUG - Checking ModelState for CompanyName/CompanyAddress errors:");
            if (ModelState.ContainsKey("CompanyName"))
            {
                foreach (var error in ModelState["CompanyName"].Errors)
                {
                    Console.WriteLine($"  ❌ ModelState CompanyName Error: {error.ErrorMessage}");
                }
            }
            if (ModelState.ContainsKey("CompanyAddress"))
            {
                foreach (var error in ModelState["CompanyAddress"].Errors)
                {
                    Console.WriteLine($"  ❌ ModelState CompanyAddress Error: {error.ErrorMessage}");
                }
            }

            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");
            
            if (HttpContext.Session.GetString("Role") != "User")
            {
                TempData["ErrorMessage"] = "Hanya user yang dapat membuat permohonan";
                return RedirectToAction("Index");
            }

            // Validate model is not null
            if (model == null)
            {
                TempData["ErrorMessage"] = "Data permohonan tidak valid";
                return RedirectToAction("Index");
            }

            // Ensure LivestockDetails is initialized
            if (model.LivestockDetails == null)
            {
                model.LivestockDetails = new List<LivestockDetailViewModel>();
            }

            try
            {
                Console.WriteLine($"🚀 Starting permit creation for user {userId.Value}");

                // Test database connection
                try
                {
                    await _context.Database.CanConnectAsync();
                    Console.WriteLine("✅ Database connection test passed");
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"❌ Database connection failed: {dbEx.Message}");
                    ModelState.AddModelError("", "Koneksi database tidak tersedia. Silakan coba lagi dalam beberapa saat.");
                    return View(model);
                }

                // ===============================================
                // STEP 1: VALIDATE USER EXISTS
                // ===============================================
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    ModelState.AddModelError("", "User tidak ditemukan");
                    Console.WriteLine("❌ User not found in database");
                    return View(model);
                }

                            // ===============================================
            // STEP 2: PROCESS APPLICANT DATA
            // ===============================================
            if (string.IsNullOrEmpty(model.ApplicantType))
            {
                ModelState.AddModelError("ApplicantType", "Tipe pemohon harus dipilih");
            }
            
            // Clear any existing validation errors for CompanyName/CompanyAddress if ApplicantType is Individual
            if (model.ApplicantType == "Individual")
            {
                Console.WriteLine("🔍 DEBUG - Clearing CompanyName/CompanyAddress validation errors for Individual applicant");
                ModelState.Remove("CompanyName");
                ModelState.Remove("CompanyAddress");
            }

                Console.WriteLine($"🔍 ApplicantType: '{model.ApplicantType}'");

                // Process applicant data based on type
                if (model.ApplicantType == "Individual")
                {
                    Console.WriteLine("👤 Processing Individual applicant...");
                    // Individual validation and mapping will be done in STEP 3
                }
                else if (model.ApplicantType == "Company")
                {
                    Console.WriteLine("🏢 Processing Company applicant...");

                    // Debug logging for CompanyName
                    Console.WriteLine($"🔍 DEBUG - CompanyName value: '{model.CompanyName}'");
                    Console.WriteLine($"🔍 DEBUG - CompanyName length: {model.CompanyName?.Length ?? 0}");
                    Console.WriteLine($"🔍 DEBUG - CompanyName is null: {model.CompanyName == null}");
                    Console.WriteLine($"🔍 DEBUG - CompanyName is empty: {string.IsNullOrEmpty(model.CompanyName)}");
                    Console.WriteLine($"🔍 DEBUG - CompanyName is whitespace: {string.IsNullOrWhiteSpace(model.CompanyName)}");
                    Console.WriteLine($"🔍 DEBUG - CompanyName trimmed: '{model.CompanyName?.Trim()}'");

                    // Validate company fields - ONLY for Company applicant type
                    if (string.IsNullOrWhiteSpace(model.CompanyName))
                    {
                        ModelState.AddModelError("CompanyName", "The Nama Perusahaan field is required.");
                        Console.WriteLine("❌ CompanyName validation failed - field is empty or whitespace");
                    }
                    else
                    {
                        Console.WriteLine("✅ CompanyName validation passed");
                    }

                    if (string.IsNullOrWhiteSpace(model.CompanyProvince))
                    {
                        ModelState.AddModelError("CompanyProvince", "Provinsi perusahaan wajib diisi");
                    }

                    if (string.IsNullOrWhiteSpace(model.CompanyRegency))
                    {
                        ModelState.AddModelError("CompanyRegency", "Kabupaten perusahaan wajib diisi");
                    }

                    // Build company address from components
                    var addressParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(model.AddressStreet)) addressParts.Add(model.AddressStreet);
                    if (!string.IsNullOrWhiteSpace(model.AddressRT) || !string.IsNullOrWhiteSpace(model.AddressRW))
                    {
                        addressParts.Add($"RT {model.AddressRT ?? "-"} / RW {model.AddressRW ?? "-"}");
                    }
                    if (!string.IsNullOrWhiteSpace(model.AddressVillage)) addressParts.Add($"Desa/Kel. {model.AddressVillage}");
                    if (!string.IsNullOrWhiteSpace(model.AddressDistrict)) addressParts.Add($"Kec. {model.AddressDistrict}");
                    if (!string.IsNullOrWhiteSpace(model.CompanyRegency)) addressParts.Add(model.CompanyRegency);
                    if (!string.IsNullOrWhiteSpace(model.CompanyProvince)) addressParts.Add(model.CompanyProvince);
                    if (!string.IsNullOrWhiteSpace(model.AddressPostalCode)) addressParts.Add(model.AddressPostalCode);

                    model.CompanyAddress = string.Join(", ", addressParts);

                    if (string.IsNullOrWhiteSpace(model.CompanyAddress))
                    {
                        ModelState.AddModelError("CompanyAddress", "Alamat perusahaan harus diisi");
                    }

                    Console.WriteLine($"✅ Company data processed - Name: '{model.CompanyName}', Address: '{model.CompanyAddress}'");
                }

                // ===============================================
                // STEP 3: VALIDATE BASIC FIELDS
                // ===============================================
                // For Individual, validate individual fields first, then map to company fields
                if (model.ApplicantType == "Individual")
                {
                    Console.WriteLine("👤 Processing Individual applicant...");
                    
                    if (string.IsNullOrWhiteSpace(model.IndividualName))
                    {
                        ModelState.AddModelError("IndividualName", "Nama lengkap wajib diisi");
                        Console.WriteLine("❌ IndividualName validation failed");
                    }
                    else
                    {
                        Console.WriteLine("✅ IndividualName validation passed");
                    }

                    if (string.IsNullOrWhiteSpace(model.IndividualProvince))
                    {
                        ModelState.AddModelError("IndividualProvince", "Provinsi wajib diisi");
                        Console.WriteLine("❌ IndividualProvince validation failed");
                    }
                    else
                    {
                        Console.WriteLine("✅ IndividualProvince validation passed");
                    }

                    if (string.IsNullOrWhiteSpace(model.IndividualRegency))
                    {
                        ModelState.AddModelError("IndividualRegency", "Kabupaten wajib diisi");
                        Console.WriteLine("❌ IndividualRegency validation failed");
                    }
                    else
                    {
                        Console.WriteLine("✅ IndividualRegency validation passed");
                    }

                    if (string.IsNullOrWhiteSpace(model.IndividualAddress))
                    {
                        ModelState.AddModelError("IndividualAddress", "Alamat lengkap wajib diisi");
                        Console.WriteLine("❌ IndividualAddress validation failed");
                    }
                    else
                    {
                        Console.WriteLine("✅ IndividualAddress validation passed");
                    }

                }
                // Note: Company validation is already handled in STEP 2 above

                if (string.IsNullOrWhiteSpace(model.OriginLocation))
                {
                    ModelState.AddModelError("OriginLocation", "Asal ternak harus diisi");
                }

                if (string.IsNullOrWhiteSpace(model.DestinationLocation))
                {
                    ModelState.AddModelError("DestinationLocation", "Tujuan pengiriman harus diisi");
                }

                if (string.IsNullOrWhiteSpace(model.DeparturePort))
                {
                    ModelState.AddModelError("DeparturePort", "Pelabuhan asal harus diisi");
                }

                if (string.IsNullOrWhiteSpace(model.ArrivalPort))
                {
                    ModelState.AddModelError("ArrivalPort", "Pelabuhan tujuan harus diisi");
                }

                // ===============================================
                // STEP 4: VALIDATE LIVESTOCK DETAILS
                // ===============================================
                Console.WriteLine("🐄 Validating livestock details...");
                var validLivestockDetails = model.LivestockDetails
                    .Where(d => !string.IsNullOrEmpty(d.LivestockType?.Trim()) && d.Quantity > 0)
                    .ToList();

                if (!validLivestockDetails.Any())
                {
                    ModelState.AddModelError("", "Minimal harus ada satu detail ternak yang valid");
                    Console.WriteLine("❌ Livestock validation failed - no valid livestock details");
                }
                else
                {
                    Console.WriteLine($"✅ Livestock validation passed - {validLivestockDetails.Count} valid entries");
                }

                // ===============================================
                // STEP 5: VALIDATE DOCUMENTS
                // ===============================================
                Console.WriteLine("📄 Validating documents...");
                var documentValidation = _documentService.ValidateAllRequiredDocuments(model);
                if (!documentValidation.IsValid)
                {
                    foreach (var error in documentValidation.Errors)
                    {
                        ModelState.AddModelError("", error);
                        Console.WriteLine($"❌ Document validation error: {error}");
                    }
                }
                else
                {
                    Console.WriteLine("✅ Document validation passed");
                }

                var detailsValidation = _documentService.ValidateDocumentDetails(model);
                if (!detailsValidation.IsValid)
                {
                    foreach (var error in detailsValidation.Errors)
                    {
                        ModelState.AddModelError("", error);
                        Console.WriteLine($"❌ Document details validation error: {error}");
                    }
                }
                else
                {
                    Console.WriteLine("✅ Document details validation passed");
                }

                // ===============================================
                // STEP 6: CHECK MODELSTATE AND RETURN IF ERRORS
                // ===============================================
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("❌ ModelState validation failed:");
                    foreach (var modelError in ModelState)
                    {
                        foreach (var error in modelError.Value.Errors)
                        {
                            Console.WriteLine($"❌ ModelState Error - Field: {modelError.Key}, Error: {error.ErrorMessage}");
                        }
                    }

                    // Ensure there's at least one livestock detail for the form
                    if (model.LivestockDetails == null || !model.LivestockDetails.Any())
                    {
                        model.LivestockDetails.Add(new LivestockDetailViewModel());
                    }

                    return View(model);
                }

                // ===============================================
                // STEP 7: GENERATE APPLICATION NUMBER
                // ===============================================
                Console.WriteLine("🔢 Generating application number...");
                string applicationNumber;
                try
                {
                    applicationNumber = await _applicationNumberService.GenerateApplicationNumberAsync();
                    Console.WriteLine($"✅ Application number generated: {applicationNumber}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to generate application number: {ex.Message}");
                    ModelState.AddModelError("", "Gagal menghasilkan nomor aplikasi. Silakan coba lagi.");
                    return View(model);
                }

                // ===============================================
                // STEP 8: CREATE PERMIT APPLICATION
                // ===============================================
                Console.WriteLine("📝 Creating permit application object...");
                
                var permitApplication = new LivestockPermitApplication
                {
                    ApplicationNumber = applicationNumber,
                    UserId = userId.Value,
                    CompanyName = model.ApplicantType == "Individual"
                        ? model.IndividualName?.Trim()
                        : model.CompanyName?.Trim(),
                    CompanyAddress = model.ApplicantType == "Individual"
                        ? model.IndividualAddress?.Trim()
                        : model.CompanyAddress?.Trim(),
                    OriginLocation = model.OriginLocation.Trim(),
                    DestinationLocation = model.DestinationLocation.Trim(),
                    DeparturePort = model.DeparturePort.Trim(),
                    ArrivalPort = model.ArrivalPort.Trim(),
                    Status = PermitStatus.Submitted,
                    SubmissionDate = DateTime.Now,
                    CurrentApprovalLevel = 1,
                    OriginProvinceId = model.OriginProvinceId > 0 ? model.OriginProvinceId : null,
                    OriginRegencyId = model.OriginRegencyId > 0 ? model.OriginRegencyId : null,
                    DestinationProvinceId = model.DestinationProvinceId > 0 ? model.DestinationProvinceId : null,
                    DestinationRegencyId = model.DestinationRegencyId > 0 ? model.DestinationRegencyId : null
                };

                // Add livestock details
                foreach (var livestockDetail in validLivestockDetails)
                {
                    permitApplication.LivestockDetails.Add(new LivestockDetail
                    {
                        LivestockType = livestockDetail.LivestockType.Trim(),
                        Quantity = livestockDetail.Quantity,
                        Description = livestockDetail.Description?.Trim()
                    });
                }

                // ===============================================
                // STEP 9: SAVE TO DATABASE
                // ===============================================
                Console.WriteLine($"📝 Saving permit application to database...");
                try
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    
                    try
                    {
                        _context.PermitApplications.Add(permitApplication);
                        await _context.SaveChangesAsync();
                        
                        await transaction.CommitAsync();
                        Console.WriteLine($"✅ Permit application saved with ID: {permitApplication.Id}");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to save permit application: {ex.Message}");
                    ModelState.AddModelError("", $"Gagal menyimpan permohonan ke database: {ex.Message}");
                    return View(model);
                }

                // ===============================================
                // STEP 10: UPLOAD DOCUMENTS
                // ===============================================
                Console.WriteLine("📎 Uploading supporting documents...");
                DocumentUploadResult uploadResult;
                try
                {
                    uploadResult = await _documentService.UploadSupportingDocumentsAsync(permitApplication.Id, model, userId.Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Document upload service error: {ex.Message}");
                    
                    // Cleanup: remove the permit application if document upload fails
                    try
                    {
                        _context.PermitApplications.Remove(permitApplication);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception cleanupEx)
                    {
                        Console.WriteLine($"⚠️ Failed to cleanup permit application: {cleanupEx.Message}");
                    }

                    ModelState.AddModelError("", "Gagal mengupload dokumen pendukung. Silakan coba lagi.");
                    return View(model);
                }

                if (!uploadResult.Success)
                {
                    Console.WriteLine($"❌ Document upload failed: {uploadResult.ErrorMessage}");
                    
                    // Cleanup: remove the permit application if document upload fails
                    try
                    {
                        _context.PermitApplications.Remove(permitApplication);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception cleanupEx)
                    {
                        Console.WriteLine($"⚠️ Failed to cleanup permit application: {cleanupEx.Message}");
                    }

                    ModelState.AddModelError("", uploadResult.ErrorMessage);
                    return View(model);
                }

                Console.WriteLine($"✅ Documents uploaded successfully: {uploadResult.UploadedCount} files");

                // ===============================================
                // STEP 11: ADD APPROVAL HISTORY
                // ===============================================
                try
                {
                    var approvalHistory = new PermitApprovalHistory
                    {
                        PermitApplicationId = permitApplication.Id,
                        UserId = userId.Value,
                        FromStatus = PermitStatus.Draft,
                        ToStatus = PermitStatus.Submitted,
                        Action = "Submit",
                        Comments = model.ApplicantType == "Individual"
                            ? $"Permohonan izin diajukan oleh pemohon perorangan: {model.IndividualName?.Trim()}"
                            : $"Permohonan izin diajukan oleh perusahaan: {model.CompanyName?.Trim()}",
                        ActionDate = DateTime.Now
                    };

                    _context.PermitApprovalHistories.Add(approvalHistory);
                    await _context.SaveChangesAsync();
                    Console.WriteLine("✅ Approval history added");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to add approval history: {ex.Message}");
                    // Don't fail the entire process for this
                }

                // ===============================================
                // STEP 12: SEND EMAIL NOTIFICATIONS
                // ===============================================
                try
                {
                    Console.WriteLine("📧 Sending email notifications...");
                    await _approvalService.SendNewPermitNotificationsAsync(permitApplication.Id);
                    Console.WriteLine("✅ Email notifications sent");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error sending email notification: {ex.Message}");
                }

                // ===============================================
                // STEP 13: SUCCESS
                // ===============================================
                var applicantTypeText = model.ApplicantType == "Individual" ? "perorangan" : "perusahaan";
                var applicantName = model.ApplicantType == "Individual"
                    ? model.IndividualName?.Trim()
                    : model.CompanyName?.Trim();

                TempData["SuccessMessage"] = $"Permohonan izin berhasil diajukan dengan nomor {applicationNumber}. " +
                                           $"Pemohon: {applicantName} ({applicantTypeText}). " +
                                           $"Total {uploadResult.UploadedCount} dokumen pendukung telah diupload. " +
                                           "Permohonan Anda akan segera diproses oleh tim admin.";

                Console.WriteLine($"✅ Permit application created successfully - ID: {permitApplication.Id}, Number: {applicationNumber}");

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Create method: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");

                ModelState.AddModelError("", "Terjadi kesalahan saat menyimpan permohonan. Silakan coba lagi.");

                if (model?.LivestockDetails == null || !model.LivestockDetails.Any())
                {
                    model?.LivestockDetails.Add(new LivestockDetailViewModel());
                }

                return View(model);
            }
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
        
        private (bool IsValid, List<string> Errors) ValidateRequiredDocuments(PermitApplicationViewModel model)
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

            return (errors.Count == 0, errors);
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

            // Check for potentially dangerous filenames
            var fileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                errors.Add("Nama file tidak valid");
            }
            else
            {
                // Check for dangerous characters and patterns
                var dangerousPatterns = new[] { "..", "/", "\\", ":", "*", "?", "\"", "<", ">", "|" };
                if (dangerousPatterns.Any(pattern => fileName.Contains(pattern)))
                {
                    errors.Add("Nama file mengandung karakter yang tidak diizinkan");
                }

                // Check for executable extensions that might be disguised
                var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".scr", ".vbs", ".js" };
                var fullFileName = fileName.ToLower();
                if (dangerousExtensions.Any(ext => fullFileName.Contains(ext)))
                {
                    errors.Add("File berpotensi berbahaya");
                }
            }

            // Additional security check: verify file signature for images
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

        public async Task<IActionResult> Detail(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");

            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .Include(p => p.ApprovalHistory)
                    .ThenInclude(h => h.User)
                .Include(p => p.Admin)
                .Include(p => p.Verifikator)
                .Include(p => p.KepalaDinas)
                .Include(p => p.Documents)
                    .ThenInclude(d => d.UploadedByUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return RedirectToAction("Index");
            }

            if (userRole == "User" && permit.UserId != userId.Value)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses ke permohonan ini";
                return RedirectToAction("Index");
            }

            var model = new PermitDetailViewModel
            {
                Id = permit.Id,
                ApplicationNumber = permit.ApplicationNumber,
                CompanyName = permit.CompanyName,
                CompanyAddress = permit.CompanyAddress,
                ApplicantName = permit.User.NamaLengkap,
                ApplicantEmail = permit.User.Email,
                ApplicantPhone = permit.User.NoTelepon,
                Status = permit.Status,
                SubmissionDate = permit.SubmissionDate,
                AdminApprovalDate = permit.AdminApprovalDate,
                VerificationDate = permit.VerificationDate,
                FinalApprovalDate = permit.FinalApprovalDate,
                OriginLocation = permit.OriginLocation,
                DestinationLocation = permit.DestinationLocation,
                DeparturePort = permit.DeparturePort,
                ArrivalPort = permit.ArrivalPort,
                RejectionReason = permit.RejectionReason,
                ValidFrom = permit.ValidFrom,
                ValidUntil = permit.ValidUntil,
                GeneratedDocumentPath = permit.GeneratedDocumentPath,
                AdminName = permit.Admin?.NamaLengkap,
                VerifikatorName = permit.Verifikator?.NamaLengkap,
                KepalaDinasName = permit.KepalaDinas?.NamaLengkap,
                CurrentApprovalLevel = permit.CurrentApprovalLevel,
                LivestockDetails = permit.LivestockDetails.Select(d => new LivestockDetailViewModel
                {
                    LivestockType = d.LivestockType,
                    Quantity = d.Quantity,
                    Description = d.Description
                }).ToList(),
                ApprovalHistory = permit.ApprovalHistory.Select(h => new ApprovalHistoryViewModel
                {
                    Action = h.Action,
                    ActionBy = h.User.NamaLengkap,
                    ActionByRole = h.User.Role,
                    ActionDate = h.ActionDate,
                    Comments = h.Comments,
                    FromStatus = h.FromStatus,
                    ToStatus = h.ToStatus
                }).OrderBy(h => h.ActionDate).ToList(),
                // ENHANCED: Include document details in response
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
                    // NEW: Document details
                    DocumentDate = d.DocumentDate,
                    DocumentNumber = d.DocumentNumber,
                    DocumentDescription = d.DocumentDescription
                }).OrderBy(d => d.DocumentType).ToList(),
                CanDownload = permit.Status == PermitStatus.FinalApproved && !string.IsNullOrEmpty(permit.GeneratedDocumentPath),
                CanApprove = CanUserApprove(userRole, permit.Status)
            };

            return View(model);
        }
        #region Debug Methods

        [HttpGet]
        public async Task<IActionResult> TestUpload()
        {
            if (_environment.IsDevelopment())
            {
                var userId = GetCurrentUserId();
                if (userId == null) return RedirectToAction("Login", "Auth");

                // Create test directories
                var testPaths = new[]
                {
            Path.Combine(_environment.WebRootPath, "documents"),
            Path.Combine(_environment.WebRootPath, "documents", "supporting"),
            Path.Combine(_environment.WebRootPath, "documents", "permits")
        };

                var results = new List<string>();

                foreach (var path in testPaths)
                {
                    try
                    {
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                            results.Add($"✅ Created directory: {path}");
                        }
                        else
                        {
                            results.Add($"✅ Directory exists: {path}");
                        }

                        // Test write permissions
                        var testFile = Path.Combine(path, "test_write.txt");
                        await System.IO.File.WriteAllTextAsync(testFile, "test");
                        System.IO.File.Delete(testFile);
                        results.Add($"✅ Write permission OK: {path}");
                    }
                    catch (Exception ex)
                    {
                        results.Add($"❌ Error with {path}: {ex.Message}");
                    }
                }

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
                var documents = await _context.PermitDocuments
                    .Include(d => d.PermitApplication)
                    .Include(d => d.UploadedByUser)
                    .Where(d => !permitId.HasValue || d.PermitApplicationId == permitId.Value)
                    .ToListAsync();

                var results = new List<object>();

                foreach (var doc in documents)
                {
                    var fullPath = Path.Combine(_environment.WebRootPath, doc.FilePath.TrimStart('/'));
                    var fileExists = System.IO.File.Exists(fullPath);

                    results.Add(new
                    {
                        doc.Id,
                        doc.DocumentName,
                        doc.DocumentType,
                        doc.FilePath,
                        FullPath = fullPath,
                        FileExists = fileExists,
                        doc.FileSize,
                        ActualSize = fileExists ? new FileInfo(fullPath).Length : 0,
                        doc.UploadDate,
                        PermitNumber = doc.PermitApplication.ApplicationNumber,
                        UploadedBy = doc.UploadedByUser.NamaLengkap
                    });
                }

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
                    var cleanedCount = await _documentService.CleanupOrphanedFilesAsync();

                    TempData["SuccessMessage"] = $"Cleanup completed. {cleanedCount} files removed.";
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

            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .Include(p => p.Documents)
                    .ThenInclude(d => d.UploadedByUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return RedirectToAction("Index");
            }

            if (!CanUserApprove(userRole, permit.Status))
            {
                TempData["ErrorMessage"] = "Permohonan tidak dapat diproses pada tahap ini";
                return RedirectToAction("Detail", new { id });
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
                return RedirectToAction("Detail", new { id = document.PermitApplicationId });
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
                return RedirectToAction("Detail", new { id = document.PermitApplicationId });
            }
        }

        /// <summary>
        /// Download dokumen izin yang telah digenerate
        /// </summary>
        /// <param name="id">ID permohonan izin</param>
        /// <returns>File untuk diunduh</returns>
        [HttpGet]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");

            // Ambil data permohonan
            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .Include(p => p.Admin)
                .Include(p => p.Verifikator)
                .Include(p => p.KepalaDinas)
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return NotFound();
            }

            // Cek akses berdasarkan role
            bool canDownload = false;
            switch (userRole)
            {
                case "User":
                    canDownload = permit.UserId == userId.Value && permit.Status >= PermitStatus.AdminApproved;
                    break;
                case "Admin":
                    canDownload = permit.Status >= PermitStatus.AdminApproved;
                    break;
                case "Verifikator":
                    canDownload = permit.Status >= PermitStatus.AdminApproved;
                    break;
                case "KepalaDinas":
                    canDownload = permit.Status >= PermitStatus.VerifikatorApproved;
                    break;
            }

            if (!canDownload)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengunduh dokumen ini";
                return Forbid();
            }

            try
            {
                // Cek apakah sudah ada file yang digenerate
                if (!string.IsNullOrEmpty(permit.GeneratedDocumentPath))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, permit.GeneratedDocumentPath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                        var fileName = Path.GetFileName(permit.GeneratedDocumentPath);
                        
                        // Return file yang sudah ada
                        return File(fileBytes, "text/html", fileName);
                    }
                }

                // Jika belum ada file, generate baru
                var htmlContent = await _pdfGenerator.GeneratePermitPdf(permit);
                
                if (htmlContent == null || htmlContent.Length == 0)
                {
                    TempData["ErrorMessage"] = "Gagal generate konten dokumen";
                    return RedirectToAction("Detail", new { id = id });
                }

                // Generate nama file
                var newFileName = $"permit_{permit.ApplicationNumber}_{DateTime.Now:yyyyMMddHHmmss}.html";
                
                // Return file untuk download sebagai HTML
                return File(htmlContent, "text/html", newFileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mengunduh dokumen";
                return RedirectToAction("Detail", new { id = id });
            }
        }

        [HttpPost]
        [Route("Permit/UpdateDocumentDetails")]
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
        public class EditDocumentDetailsRequest
        {
            public int DocumentId { get; set; }
            public DateTime? DocumentDate { get; set; }
            public string DocumentNumber { get; set; }
            public string DocumentDescription { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> ViewDocument(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");

            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .Include(p => p.Admin)
                .Include(p => p.Verifikator)
                .Include(p => p.KepalaDinas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                return NotFound();
            }

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
                    canView = permit.UserId == userId.Value;
                    break;
            }

            if (!canView)
            {
                return Forbid();
            }

            if (string.IsNullOrEmpty(permit.GeneratedDocumentPath))
            {
                await GeneratePermitDocument(permit);
                await _context.SaveChangesAsync();
            }

            var viewModel = new PermitDocumentViewModel
            {
                PermitId = permit.Id,
                ApplicationNumber = permit.ApplicationNumber,
                CompanyName = permit.CompanyName,
                Status = permit.Status,
                DocumentContent = await GetDocumentContent(permit),
                CanApprove = CanUserApprove(userRole, permit.Status),
                UserRole = userRole
            };

            return View(viewModel);
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

                // Generate fresh content if file doesn't exist
                var htmlBytes = await _pdfGenerator.GeneratePermitPdf(permit);
                return System.Text.Encoding.UTF8.GetString(htmlBytes);
            }
            catch (Exception)
            {
                return "<div class='alert alert-danger'><i class='fas fa-exclamation-triangle'></i> Gagal memuat konten dokumen.</div>";
            }
        }

        // Method untuk API endpoint yang mengembalikan file binary (untuk modal/iframe)
        [HttpGet]
        public async Task<IActionResult> GetDocumentFile(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .Include(p => p.Admin)
                .Include(p => p.Verifikator)
                .Include(p => p.KepalaDinas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                return NotFound();
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
                    canView = permit.UserId == userId.Value;
                    break;
            }

            if (!canView)
            {
                return Forbid();
            }

            try
            {
                // Generate document if doesn't exist
                if (string.IsNullOrEmpty(permit.GeneratedDocumentPath))
                {
                    await GeneratePermitDocument(permit);
                    await _context.SaveChangesAsync();
                }

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", permit.GeneratedDocumentPath.TrimStart('/'));

                if (!System.IO.File.Exists(filePath))
                {
                    // Regenerate if missing
                    await GeneratePermitDocument(permit);
                    await _context.SaveChangesAsync();
                    filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", permit.GeneratedDocumentPath.TrimStart('/'));
                }

                if (System.IO.File.Exists(filePath))
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                    return File(fileBytes, "text/html");
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception)
            {
                return StatusCode(500, "Error loading document");
            }
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
                var result = await _approvalService.ProcessApprovalAsync(
                    model.Id,
                    model.Action,
                    model.Comments,
                    userId.Value,
                    userRole);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Permohonan {result.PermitApplicationNumber} berhasil {result.ActionText.ToLower()}";
                    return RedirectToAction("Index");
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
        public async Task<IActionResult> Download(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");

            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .Include(p => p.Admin)
                .Include(p => p.Verifikator)
                .Include(p => p.KepalaDinas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return RedirectToAction("Index");
            }

            if (userRole != "User")
            {
                TempData["ErrorMessage"] = "Download hanya tersedia untuk pemohon";
                return RedirectToAction("Detail", new { id });
            }

            if (permit.UserId != userId.Value)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses ke dokumen ini";
                return RedirectToAction("Index");
            }


            if (permit.Status != PermitStatus.FinalApproved || string.IsNullOrEmpty(permit.GeneratedDocumentPath))
            {
                TempData["ErrorMessage"] = "Dokumen belum tersedia untuk didownload. Menunggu persetujuan akhir.";
                return RedirectToAction("Detail", new { id });
            }

            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", permit.GeneratedDocumentPath.TrimStart('/'));

                if (!System.IO.File.Exists(filePath))
                {
                    // File tidak ada, regenerate
                    TempData["ErrorMessage"] = "File dokumen tidak ditemukan. Sedang memproses ulang...";
                    await RegeneratePermitDocument(permit);
                    return RedirectToAction("Detail", new { id });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var fileName = $"Izin_Pengeluaran_Ternak_{permit.ApplicationNumber.Replace("/", "_")}.html";

                // Return as HTML file that can be opened in browser and printed as PDF
                return File(fileBytes, "text/html", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mengunduh dokumen";
                return RedirectToAction("Detail", new { id });
            }
        }

        #region Private Methods

        private int? GetCurrentUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }

        // Keep as wrapper untuk backward compatibility:
        private bool CanUserApprove(string userRole, PermitStatus status)
        {
            return _approvalService.CanUserApprove(userRole, status);
        }
        private async Task GeneratePermitDocument(LivestockPermitApplication permit)
        {
            try
            {
                var htmlBytes = await _pdfGenerator.GeneratePermitPdf(permit);

                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents", "permits");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var fileName = $"permit_{permit.ApplicationNumber.Replace("/", "_")}_{DateTime.Now:yyyyMMddHHmmss}.html";
                var filePath = Path.Combine(uploadsPath, fileName);

                await System.IO.File.WriteAllBytesAsync(filePath, htmlBytes);

                // Update permit with document path
                permit.GeneratedDocumentPath = $"/documents/permits/{fileName}";
            }
            catch (Exception ex)
            {
                // Log error but don't fail the approval process
                Console.WriteLine($"Error generating document: {ex.Message}");

                // Set a placeholder path to indicate document generation was attempted
                permit.GeneratedDocumentPath = $"/documents/permits/error_{permit.Id}_{DateTime.Now:yyyyMMddHHmmss}.html";
            }
        }

        // Method to regenerate document if missing
        private async Task RegeneratePermitDocument(LivestockPermitApplication permit)
        {
            try
            {
                await GeneratePermitDocument(permit);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error regenerating document: {ex.Message}");
            }
        }

        #endregion

        // Tambahkan method-method berikut ke dalam PermitController.cs

        #region Helper Methods for Index DataTable

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
                var query = GetPermitsQuery(userRole, userId.Value);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p =>
                        p.ApplicationNumber.Contains(search) ||
                        p.CompanyName.Contains(search) ||
                        p.User.NamaLengkap.Contains(search) ||
                        p.OriginLocation.Contains(search) ||
                        p.DestinationLocation.Contains(search));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    if (Enum.TryParse<PermitStatus>(statusFilter, out var status))
                    {
                        query = query.Where(p => p.Status == status);
                    }
                }

                // Apply date filter
                if (!string.IsNullOrEmpty(dateFilter))
                {
                    if (DateTime.TryParse(dateFilter, out var filterDate))
                    {
                        query = query.Where(p => p.SubmissionDate.Date == filterDate.Date);
                    }
                }

                var totalRecords = await query.CountAsync();

                // Apply pagination
                var permits = await query
                    .OrderByDescending(p => p.SubmissionDate)
                    .Skip(start)
                    .Take(length)
                    .Select(p => new
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = p.Status.ToString(),
                        StatusText = PermitStatusHelper.GetStatusText(p.Status),
                        StatusClass = PermitStatusHelper.GetStatusClass(p.Status),
                        ProgressPercentage = PermitStatusHelper.GetProgressPercentage(p.Status),
                        ProgressText = PermitStatusHelper.GetProgressText(p.Status),
                        SubmissionDate = p.SubmissionDate.ToString("dd/MM/yyyy"),
                        SubmissionTime = p.SubmissionDate.ToString("HH:mm"),
                        OriginLocation = p.OriginLocation,
                        DestinationLocation = p.DestinationLocation,
                        DocumentCount = p.Documents.Count,
                        CanApprove = CanUserApprove(userRole, p.Status),
                        CanDownload = p.Status == PermitStatus.FinalApproved && !string.IsNullOrEmpty(p.GeneratedDocumentPath) && userRole == "User",
                        HasDocument = !string.IsNullOrEmpty(p.GeneratedDocumentPath),
                        GeneratedDocumentPath = p.GeneratedDocumentPath
                    })
                    .ToListAsync();

                return Json(new
                {
                    draw = draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = permits
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

        /// <summary>
        /// Get permits query based on user role
        /// </summary>
        private IQueryable<LivestockPermitApplication> GetPermitsQuery(string userRole, int userId)
        {
            var baseQuery = _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.Documents)
                .AsQueryable();

            return userRole switch
            {
                "User" => baseQuery.Where(p => p.UserId == userId),
                "Admin" => baseQuery.Where(p => p.Status == PermitStatus.Submitted || p.Status == PermitStatus.UnderAdminReview),
                "Verifikator" => baseQuery.Where(p => p.Status == PermitStatus.AdminApproved || p.Status == PermitStatus.UnderVerifikatorReview),
                "KepalaDinas" => baseQuery.Where(p => p.Status == PermitStatus.VerifikatorApproved || p.Status == PermitStatus.PendingKepalaDinas),
                _ => baseQuery.Where(p => false) // No access
            };
        }

        /// <summary>
        /// API endpoint untuk mendapatkan statistik permits
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPermitStatistics()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var query = GetPermitsQuery(userRole, userId.Value);
                var permits = await query.ToListAsync();

                var stats = new
                {
                    total = permits.Count,
                    pending = permits.Count(p => p.Status == PermitStatus.Submitted ||
                                               p.Status == PermitStatus.UnderAdminReview ||
                                               p.Status == PermitStatus.UnderVerifikatorReview ||
                                               p.Status == PermitStatus.PendingKepalaDinas),
                    approved = permits.Count(p => p.Status == PermitStatus.FinalApproved),
                    rejected = permits.Count(p => p.Status == PermitStatus.AdminRejected ||
                                                p.Status == PermitStatus.VerifikatorRejected ||
                                                p.Status == PermitStatus.KepalaDinasRejected),
                    inProcess = permits.Count(p => p.Status == PermitStatus.AdminApproved ||
                                                 p.Status == PermitStatus.VerifikatorApproved),

                    // Monthly statistics
                    thisMonth = permits.Count(p => p.SubmissionDate.Month == DateTime.Now.Month &&
                                                 p.SubmissionDate.Year == DateTime.Now.Year),
                    thisWeek = permits.Count(p => p.SubmissionDate >= DateTime.Now.AddDays(-7)),
                    today = permits.Count(p => p.SubmissionDate.Date == DateTime.Today),

                    // Average processing time
                    avgProcessingDays = CalculateAverageProcessingTime(permits)
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Calculate average processing time for completed permits
        /// </summary>
        private double CalculateAverageProcessingTime(List<LivestockPermitApplication> permits)
        {
            var completedPermits = permits.Where(p =>
                p.Status == PermitStatus.FinalApproved &&
                p.FinalApprovalDate.HasValue).ToList();

            if (!completedPermits.Any()) return 0;

            var totalDays = completedPermits.Sum(p =>
                (p.FinalApprovalDate.Value - p.SubmissionDate).TotalDays);

            return Math.Round(totalDays / completedPermits.Count, 1);
        }

        /// <summary>
        /// Export permits data to CSV
        /// </summary>
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
                var query = GetPermitsQuery(userRole, userId.Value);

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p =>
                        p.ApplicationNumber.Contains(search) ||
                        p.CompanyName.Contains(search) ||
                        p.User.NamaLengkap.Contains(search));
                }

                if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<PermitStatus>(statusFilter, out var status))
                {
                    query = query.Where(p => p.Status == status);
                }

                if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
                {
                    query = query.Where(p => p.SubmissionDate >= fromDate);
                }

                if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var toDate))
                {
                    query = query.Where(p => p.SubmissionDate <= toDate.AddDays(1));
                }

                var permits = await query
                    .OrderByDescending(p => p.SubmissionDate)
                    .ToListAsync();

                var exportData = permits.Select(p => new
                {
                    p.ApplicationNumber,
                    p.CompanyName,
                    ApplicantName = p.User.NamaLengkap,
                    Status = PermitStatusHelper.GetStatusText(p.Status),
                    SubmissionDate = p.SubmissionDate.ToString("dd/MM/yyyy HH:mm"),
                    p.OriginLocation,
                    p.DestinationLocation,
                    DocumentCount = p.Documents.Count,
                    AdminApprovalDate = p.AdminApprovalDate.HasValue ? p.AdminApprovalDate.Value.ToString("dd/MM/yyyy HH:mm") : "",
                    VerificationDate = p.VerificationDate.HasValue ? p.VerificationDate.Value.ToString("dd/MM/yyyy HH:mm") : "",
                    FinalApprovalDate = p.FinalApprovalDate.HasValue ? p.FinalApprovalDate.Value.ToString("dd/MM/yyyy HH:mm") : ""
                }).ToList();

                var csv = new StringBuilder();
                csv.AppendLine("No. Aplikasi,Perusahaan,Pemohon,Status,Tanggal Pengajuan,Asal,Tujuan,Jumlah Dokumen,Persetujuan Admin,Verifikasi,Persetujuan Final");

                foreach (var permit in exportData)
                {
                    csv.AppendLine($"\"{permit.ApplicationNumber}\",\"{permit.CompanyName}\",\"{permit.ApplicantName}\",\"{permit.Status}\",\"{permit.SubmissionDate}\",\"{permit.OriginLocation}\",\"{permit.DestinationLocation}\",\"{permit.DocumentCount}\",\"{permit.AdminApprovalDate}\",\"{permit.VerificationDate}\",\"{permit.FinalApprovalDate}\"");
                }

                var fileName = $"daftar_permohonan_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var bytes = Encoding.UTF8.GetBytes(csv.ToString());

                return File(bytes, "text/csv", fileName);
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

        /// <summary>
        /// Process individual approval/rejection
        /// </summary>
        private async Task<(bool Success, string ErrorMessage)> ProcessApproval(
            LivestockPermitApplication permit,
            string action,
            string comments,
            int userId,
            string userRole)
        {
            try
            {
                var fromStatus = permit.Status;
                PermitStatus toStatus;
                string actionText;

                if (action == "Approve")
                {
                    if (userRole == "Admin")
                    {
                        toStatus = PermitStatus.AdminApproved;
                        actionText = "Disetujui Admin";
                        permit.AdminId = userId;
                        permit.AdminApprovalDate = DateTime.Now;
                        permit.CurrentApprovalLevel = 2;

                        // Generate PDF document setelah admin approve
                        await GeneratePermitDocument(permit);
                    }
                    else if (userRole == "Verifikator")
                    {
                        toStatus = PermitStatus.VerifikatorApproved;
                        actionText = "Disetujui Verifikator";
                        permit.VerifikatorId = userId;
                        permit.VerificationDate = DateTime.Now;
                        permit.CurrentApprovalLevel = 3;
                    }
                    else // KepalaDinas
                    {
                        toStatus = PermitStatus.FinalApproved;
                        actionText = "Disetujui Kepala Dinas";
                        permit.KepalaDinasId = userId;
                        permit.FinalApprovalDate = DateTime.Now;
                        permit.ValidFrom = DateTime.Now;
                        permit.ValidUntil = DateTime.Now.AddMonths(6);
                        permit.CurrentApprovalLevel = 4;
                    }
                }
                else // Reject
                {
                    if (userRole == "Admin")
                    {
                        toStatus = PermitStatus.AdminRejected;
                        actionText = "Ditolak Admin";
                    }
                    else if (userRole == "Verifikator")
                    {
                        toStatus = PermitStatus.VerifikatorRejected;
                        actionText = "Ditolak Verifikator";
                    }
                    else // KepalaDinas
                    {
                        toStatus = PermitStatus.KepalaDinasRejected;
                        actionText = "Ditolak Kepala Dinas";
                    }

                    permit.RejectionReason = comments;
                }

                permit.Status = toStatus;

                // Add approval history
                var history = new PermitApprovalHistory
                {
                    PermitApplicationId = permit.Id,
                    UserId = userId,
                    FromStatus = fromStatus,
                    ToStatus = toStatus,
                    Action = actionText,
                    Comments = comments ?? $"Bulk {action.ToLower()}",
                    ActionDate = DateTime.Now
                };

                _context.PermitApprovalHistories.Add(history);

                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Get permit progress steps for display
        /// </summary>
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

        /// <summary>
        /// Advanced search with multiple filters
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AdvancedSearch([FromBody] AdvancedSearchRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var query = GetPermitsQuery(userRole, userId.Value);

                // Apply advanced filters
                if (!string.IsNullOrEmpty(request.ApplicationNumber))
                {
                    query = query.Where(p => p.ApplicationNumber.Contains(request.ApplicationNumber));
                }

                if (!string.IsNullOrEmpty(request.CompanyName))
                {
                    query = query.Where(p => p.CompanyName.Contains(request.CompanyName));
                }

                if (!string.IsNullOrEmpty(request.ApplicantName))
                {
                    query = query.Where(p => p.User.NamaLengkap.Contains(request.ApplicantName));
                }

                if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<PermitStatus>(request.Status, out var status))
                {
                    query = query.Where(p => p.Status == status);
                }

                if (!string.IsNullOrEmpty(request.OriginLocation))
                {
                    query = query.Where(p => p.OriginLocation.Contains(request.OriginLocation));
                }

                if (!string.IsNullOrEmpty(request.DestinationLocation))
                {
                    query = query.Where(p => p.DestinationLocation.Contains(request.DestinationLocation));
                }

                if (request.DateFrom.HasValue)
                {
                    query = query.Where(p => p.SubmissionDate >= request.DateFrom.Value);
                }

                if (request.DateTo.HasValue)
                {
                    query = query.Where(p => p.SubmissionDate <= request.DateTo.Value.AddDays(1));
                }

                if (request.MinDocuments.HasValue)
                {
                    query = query.Where(p => p.Documents.Count >= request.MinDocuments.Value);
                }

                var results = await query
                    .OrderByDescending(p => p.SubmissionDate)
                    .Take(100) // Limit results for performance
                    .Select(p => new
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = PermitStatusHelper.GetStatusText(p.Status),
                        SubmissionDate = p.SubmissionDate.ToString("dd/MM/yyyy"),
                        OriginLocation = p.OriginLocation,
                        DestinationLocation = p.DestinationLocation,
                        DocumentCount = p.Documents.Count
                    })
                    .ToListAsync();

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

        /// <summary>
        /// Get dashboard data for current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");

            try
            {
                var query = GetPermitsQuery(userRole, userId.Value);
                var permits = await query.ToListAsync();

                var dashboardData = new
                {
                    totalPermits = permits.Count,
                    pendingAction = permits.Count(p => CanUserApprove(userRole, p.Status)),
                    recentActivity = permits
                        .OrderByDescending(p => p.SubmissionDate)
                        .Take(5)
                        .Select(p => new
                        {
                            id = p.Id,
                            applicationNumber = p.ApplicationNumber,
                            companyName = p.CompanyName,
                            status = PermitStatusHelper.GetStatusText(p.Status),
                            statusClass = PermitStatusHelper.GetStatusClass(p.Status),
                            submissionDate = p.SubmissionDate.ToString("dd/MM/yyyy"),
                            daysAgo = (DateTime.Now - p.SubmissionDate).Days
                        }),
                    statusDistribution = permits
                        .GroupBy(p => p.Status)
                        .Select(g => new
                        {
                            status = g.Key.ToString(),
                            statusText = PermitStatusHelper.GetStatusText(g.Key),
                            count = g.Count(),
                            percentage = Math.Round((double)g.Count() / permits.Count * 100, 1)
                        })
                        .OrderByDescending(x => x.count),
                    monthlyTrend = GetMonthlyTrend(permits)
                };

                return Json(dashboardData);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get monthly trend data
        /// </summary>
        private object GetMonthlyTrend(List<LivestockPermitApplication> permits)
        {
            var monthlyData = permits
                .Where(p => p.SubmissionDate >= DateTime.Now.AddMonths(-6))
                .GroupBy(p => new { p.SubmissionDate.Year, p.SubmissionDate.Month })
                .Select(g => new
                {
                    year = g.Key.Year,
                    month = g.Key.Month,
                    monthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    submitted = g.Count(),
                    approved = g.Count(p => p.Status == PermitStatus.FinalApproved),
                    rejected = g.Count(p => PermitStatusHelper.IsRejectedStatus(p.Status))
                })
                .OrderBy(x => x.year)
                .ThenBy(x => x.month)
                .ToList();

            return monthlyData;
        }

        #endregion

        #region Request Models for API

        public class BulkActionRequest
        {
            public List<int> PermitIds { get; set; } = new();
            public string Comments { get; set; } = "";
        }

        public class AdvancedSearchRequest
        {
            public string ApplicationNumber { get; set; } = "";
            public string CompanyName { get; set; } = "";
            public string ApplicantName { get; set; } = "";
            public string Status { get; set; } = "";
            public string OriginLocation { get; set; } = "";
            public string DestinationLocation { get; set; } = "";
            public DateTime? DateFrom { get; set; }
            public DateTime? DateTo { get; set; }
            public int? MinDocuments { get; set; }
        }

        #endregion

        #region Document Details Helper Methods (ADD TO CONTROLLER)

        
        /// <summary>
        /// Validates document number format
        /// </summary>
        /// <param name="documentNumber">Document number to validate</param>
        /// <returns>True if format is valid, false otherwise</returns>
        private bool IsValidDocumentNumber(string documentNumber)
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

        /// <summary>
        /// Generates appropriate document description based on document details
        /// </summary>
        /// <param name="name">Document name</param>
        /// <param name="number">Document number</param>
        /// <param name="date">Document date</param>
        /// <returns>Generated description</returns>
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


        #endregion


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

            var document = await _context.PermitDocuments
                .Include(d => d.PermitApplication)
                .FirstOrDefaultAsync(d => d.Id == documentId);

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

            ViewBag.PermitApplicationNumber = document.PermitApplication.ApplicationNumber;
            return View(model);
        }
        [HttpPost]
        [Route("Permit/EditDocumentDetailsFromView")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDocumentDetailsFromView(DocumentDetailsViewModel model)
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
                    return RedirectToAction("Index");
                }

                // Update document details
                document.DocumentDate = model.DocumentDate;
                document.DocumentNumber = model.DocumentNumber;
                document.DocumentDescription = model.DocumentDescription;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Detail dokumen berhasil diperbarui";
                return RedirectToAction("Detail", new { id = document.PermitApplicationId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating document details: {ex.Message}");
                ModelState.AddModelError("", "Terjadi kesalahan saat memperbarui detail dokumen");
                return View("EditDocumentDetails", model);
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
        
        /// <summary>
        /// API endpoint untuk mengambil data user dengan format yang lebih spesifik
        /// untuk form individual applicant
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCurrentUserForIndividualForm()
        {
            try
            {
                var username = User.Identity.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "User tidak terautentikasi" });
                }

                var user = await _context.Users
                    .Where(u => u.Username == username)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Json(new { success = false, message = "Data user tidak ditemukan" });
                }

                // Format response khusus untuk individual form
                var individualFormData = new
                {
                    success = true,
                    data = new
                    {
                        // Data untuk form individual
                        individualName = user.NamaLengkap ?? "",
                        individualAddress = user.Alamat ?? "",
                        individualProvince = "", // Set dari session atau database jika ada
                        individualRegency = "", // Set dari session atau database jika ada

                        // Data tambahan
                        email = user.Email ?? "",
                        noTelepon = user.NoTelepon ?? "",

                        // Helper flags
                        hasAddress = !string.IsNullOrEmpty(user.Alamat),
                        hasName = !string.IsNullOrEmpty(user.NamaLengkap),
                        needsLocationData = string.IsNullOrEmpty(user.Alamat),

                        // Instructions for user
                        suggestions = GenerateFormSuggestions(user)
                    },
                    message = "Data berhasil dimuat untuk form perorangan"
                };

                return Json(individualFormData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetCurrentUserForIndividualForm: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Terjadi kesalahan saat mengambil data user"
                });
            }
        }

        /// <summary>
        /// Generate suggestions untuk user berdasarkan kelengkapan profil
        /// </summary>
        private List<string> GenerateFormSuggestions(User user)
        {
            var suggestions = new List<string>();

            if (string.IsNullOrEmpty(user.NamaLengkap))
            {
                suggestions.Add("Lengkapi nama lengkap di profil untuk mempercepat pengisian form");
            }

            if (string.IsNullOrEmpty(user.Alamat))
            {
                suggestions.Add("Tambahkan alamat di profil untuk mempercepat pengisian form");
            }

            if (string.IsNullOrEmpty(user.NoTelepon))
            {
                suggestions.Add("Tambahkan nomor telepon di profil untuk keperluan komunikasi");
            }

            if (suggestions.Count == 0)
            {
                suggestions.Add("Profil Anda sudah lengkap!");
            }

            return suggestions;
        }

        /// <summary>
        /// Bulk update user profile data
        /// Useful jika user ingin mengupdate profil dari form individual
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateUserProfileFromForm([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var username = User.Identity.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "User tidak terautentikasi" });
                }

                var user = await _context.Users
                    .Where(u => u.Username == username)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Json(new { success = false, message = "User tidak ditemukan" });
                }

                // Update only if new data is provided and different from current
                bool hasChanges = false;

                if (!string.IsNullOrEmpty(request.NamaLengkap) && user.NamaLengkap != request.NamaLengkap)
                {
                    user.NamaLengkap = request.NamaLengkap.Trim();
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(request.Alamat) && user.Alamat != request.Alamat)
                {
                    user.Alamat = request.Alamat.Trim();
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(request.NoTelepon) && user.NoTelepon != request.NoTelepon)
                {
                    user.NoTelepon = request.NoTelepon.Trim();
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"✅ Updated profile for user: {username}");

                    return Json(new
                    {
                        success = true,
                        message = "Profil berhasil diperbarui",
                        data = new
                        {
                            namaLengkap = user.NamaLengkap,
                            alamat = user.Alamat,
                            noTelepon = user.NoTelepon
                        }
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = true,
                        message = "Tidak ada perubahan data",
                        data = new
                        {
                            namaLengkap = user.NamaLengkap,
                            alamat = user.Alamat,
                            noTelepon = user.NoTelepon
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating user profile: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Terjadi kesalahan saat memperbarui profil"
                });
            }
        }

        /// <summary>
        /// Validate individual form data before submission
        /// </summary>
        [HttpPost]
        public IActionResult ValidateIndividualData([FromBody] IndividualValidationRequest request)
        {
            try
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                // Required field validation
                if (string.IsNullOrWhiteSpace(request.IndividualName))
                    errors.Add("Nama lengkap wajib diisi");

                if (string.IsNullOrWhiteSpace(request.IndividualProvince))
                    errors.Add("Provinsi wajib diisi");

                if (string.IsNullOrWhiteSpace(request.IndividualRegency))
                    errors.Add("Kabupaten/Kota wajib diisi");

                if (string.IsNullOrWhiteSpace(request.IndividualAddress))
                    errors.Add("Alamat lengkap wajib diisi");

                // Data quality validation
                if (!string.IsNullOrEmpty(request.IndividualName))
                {
                    if (request.IndividualName.Length < 3)
                        warnings.Add("Nama terlalu pendek, pastikan nama lengkap sudah benar");

                    if (request.IndividualName.Split(' ').Length < 2)
                        warnings.Add("Disarankan menggunakan nama lengkap (minimal 2 kata)");
                }

                if (!string.IsNullOrEmpty(request.IndividualAddress))
                {
                    if (request.IndividualAddress.Length < 10)
                        warnings.Add("Alamat terlalu pendek, pastikan alamat lengkap sudah benar");
                }

                // Business logic validation
                if (errors.Count == 0)
                {
                    // Check for duplicate names (optional)
                    // var duplicateCount = await _context.PermitApplications
                    //     .CountAsync(p => p.CompanyName.Contains(request.IndividualName));
                    // 
                    // if (duplicateCount > 0)
                    // {
                    //     warnings.Add($"Ditemukan {duplicateCount} permohonan dengan nama serupa");
                    // }
                }

                return Json(new
                {
                    success = true,
                    isValid = errors.Count == 0,
                    errors = errors,
                    warnings = warnings,
                    validationSummary = new
                    {
                        totalErrors = errors.Count,
                        totalWarnings = warnings.Count,
                        canProceed = errors.Count == 0,
                        severity = errors.Count > 0 ? "error" : (warnings.Count > 0 ? "warning" : "success")
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in individual validation: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Terjadi kesalahan saat validasi data"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLocationSuggestions(string query, string type = "province")
        {
            try
            {
                if (string.IsNullOrEmpty(query) || query.Length < 2)
                {
                    return Json(new { success = true, data = new List<object>() });
                }

                var suggestions = new List<object>();

                if (type.ToLower() == "province")
                {
                    var provinces = new[]
                    {
                "Nusa Tenggara Barat",
                "Nusa Tenggara Timur",
                "Bali",
                "Jawa Barat",
                "Jawa Tengah",
                "Jawa Timur",
                "DKI Jakarta"
            };

                    suggestions = provinces
                        .Where(p => p.ToLower().Contains(query.ToLower()))
                        .Select(p => new { text = p, value = p })
                        .ToList<object>();
                }
                else if (type.ToLower() == "regency")
                {
                    // Example regency suggestions for NTB
                    var regencies = new[]
                    {
                "Kota Mataram",
                "Kabupaten Lombok Barat",
                "Kabupaten Lombok Tengah",
                "Kabupaten Lombok Timur",
                "Kabupaten Lombok Utara",
                "Kabupaten Sumbawa",
                "Kabupaten Sumbawa Barat",
                "Kabupaten Dompu",
                "Kabupaten Bima",
                "Kota Bima"
            };

                    suggestions = regencies
                        .Where(r => r.ToLower().Contains(query.ToLower()))
                        .Select(r => new { text = r, value = r })
                        .ToList<object>();
                }

                return Json(new
                {
                    success = true,
                    data = suggestions,
                    query = query,
                    type = type,
                    count = suggestions.Count
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting location suggestions: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Terjadi kesalahan saat mengambil saran lokasi"
                });
            }
        }





        #region Request Models untuk API baru

        /// <summary>
        /// Request model untuk update profile
        /// </summary>
        public class UpdateProfileRequest
        {
            public string? NamaLengkap { get; set; }
            public string? Alamat { get; set; }
            public string? NoTelepon { get; set; }
            public string? Email { get; set; }
        }

        /// <summary>
        /// Request model untuk validasi data individual
        /// </summary>
        public class IndividualValidationRequest
        {
            public string? IndividualName { get; set; }
            public string? IndividualProvince { get; set; }
            public string? IndividualRegency { get; set; }
            public string? IndividualAddress { get; set; }
        }

        #endregion


        // File: Controllers/PermitController.cs
        // Update method EnableEditMode dan ApproveWithEdits

        #region Enhanced Admin Edit Functionality

        /// <summary>
        /// Enable editing mode for admin with full location and livestock edit support
        /// </summary>
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
                return RedirectToAction("Index");
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

                // Populate editable fields
                EditableCompanyName = permit.CompanyName,
                EditableCompanyAddress = permit.CompanyAddress,
                EditableOriginLocation = permit.OriginLocation,
                EditableDestinationLocation = permit.DestinationLocation,
                EditableDeparturePort = permit.DeparturePort,
                EditableArrivalPort = permit.ArrivalPort,

                // Populate location IDs - use port-based approach for better accuracy
                EditableOriginProvinceId = await GetProvinceCodeFromPortName(permit.DeparturePort),
                EditableOriginRegencyId = await GetRegencyCodeFromPortName(permit.DeparturePort),
                EditableDestinationProvinceId = await GetProvinceCodeFromPortName(permit.ArrivalPort),
                EditableDestinationRegencyId = await GetRegencyCodeFromPortName(permit.ArrivalPort),

                IsEditingData = true,

                // Convert to editable livestock details
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

            // Ensure at least one livestock entry for the form
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

        // ⭐ NEW: Enhanced location parsing method


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
                return RedirectToAction("Index");
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
                    return RedirectToAction("Index");
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
        #endregion

        #region Helper Methods for Admin Edit

        /// <summary>
        /// Apply basic field changes (company, location, ports)
        /// </summary>

        /// <summary>
        /// Apply livestock detail changes
        /// </summary>
        private async Task<List<string>> ApplyLivestockChanges(LivestockPermitApplication permit, PermitApprovalViewModel model)
        {
            var changedFields = new List<string>();

            if (model.EditableLivestockDetails == null || !model.EditableLivestockDetails.Any())
            {
                return changedFields;
            }

            // Get current livestock details
            var currentLivestock = permit.LivestockDetails.ToList();
            var originalCount = currentLivestock.Count;
            var originalSummary = string.Join(", ", currentLivestock.Select(l => $"{l.LivestockType}: {l.Quantity} ekor"));

            // Remove existing livestock details to replace with edited ones
            _context.LivestockDetails.RemoveRange(currentLivestock);

            // Add updated livestock details
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

            // Track changes
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

        /// <summary>
        /// Extract province code from location string (helper method)
        /// </summary>
        private string ExtractProvinceFromLocation(string location)
        {
            if (string.IsNullOrEmpty(location)) return "";

            // Simple extraction - in real implementation, you might want to use a lookup table
            var parts = location.Split(',');
            if (parts.Length >= 2)
            {
                var province = parts.Last().Trim();
                // Return a default code or lookup from your province data
                return GetProvinceCodeByName(province);
            }

            return "";
        }

        /// <summary>
        /// Extract regency code from location string (helper method)
        /// </summary>
        private string ExtractRegencyFromLocation(string location)
        {
            if (string.IsNullOrEmpty(location)) return "";

            Console.WriteLine($"ExtractRegencyFromLocation - Input location: '{location}'");
            
            var parts = location.Split(',');
            Console.WriteLine($"ExtractRegencyFromLocation - Parts count: {parts.Length}");
            
            if (parts.Length >= 1)
            {
                var regency = parts[0].Trim();
                Console.WriteLine($"ExtractRegencyFromLocation - Extracted regency: '{regency}'");
                
                var regencyCode = GetRegencyCodeByName(regency);
                Console.WriteLine($"ExtractRegencyFromLocation - Regency code: '{regencyCode}'");
                
                return regencyCode;
            }

            Console.WriteLine($"ExtractRegencyFromLocation - No regency found");
            return "";
        }

        /// <summary>
        /// Get province code by name (implement based on your data structure)
        /// </summary>
        private string GetProvinceCodeByName(string provinceName)
        {
            if (string.IsNullOrEmpty(provinceName))
                return "";

            // Enhanced mapping for common provinces
            var provinceMap = new Dictionary<string, string>
            {
                { "Nusa Tenggara Barat", "52" },
                { "Nusa Tenggara Timur", "53" },
                { "Bali", "51" },
                { "Jawa Timur", "35" },
                { "Jawa Tengah", "33" },
                { "Jawa Barat", "32" },
                { "DKI Jakarta", "31" },
                { "Sumatera Utara", "12" },
                { "Sumatera Barat", "13" },
                { "Riau", "14" },
                { "Jambi", "15" },
                { "Sumatera Selatan", "16" },
                { "Bengkulu", "17" },
                { "Lampung", "18" },
                { "Bangka Belitung", "19" },
                { "Kepulauan Riau", "21" },
                { "Aceh", "11" },
                { "Kalimantan Barat", "61" },
                { "Kalimantan Tengah", "62" },
                { "Kalimantan Selatan", "63" },
                { "Kalimantan Timur", "64" },
                { "Kalimantan Utara", "65" },
                { "Sulawesi Utara", "71" },
                { "Sulawesi Tengah", "72" },
                { "Sulawesi Selatan", "73" },
                { "Sulawesi Tenggara", "74" },
                { "Gorontalo", "75" },
                { "Sulawesi Barat", "76" },
                { "Maluku", "81" },
                { "Maluku Utara", "82" },
                { "Papua", "94" },
                { "Papua Barat", "91" }
            };

            return provinceMap.ContainsKey(provinceName) ? provinceMap[provinceName] : "";
        }

        /// <summary>
        /// Get regency code by name (implement based on your data structure)
        /// </summary>
        private string GetRegencyCodeByName(string regencyName)
        {
            Console.WriteLine($"GetRegencyCodeByName - Input regencyName: '{regencyName}'");
            
            if (string.IsNullOrEmpty(regencyName))
            {
                Console.WriteLine($"GetRegencyCodeByName - Empty regency name");
                return "";
            }

            // Enhanced mapping for common regencies in NTB and other provinces
            var regencyMap = new Dictionary<string, string>
            {
                // DKI Jakarta
                { "Kota Administrasi Jakarta Barat", "31.73" },
                { "Kota Administrasi Jakarta Pusat", "31.71" },
                { "Kota Administrasi Jakarta Selatan", "31.74" },
                { "Kota Administrasi Jakarta Timur", "31.75" },
                { "Kota Administrasi Jakarta Utara", "31.72" },
                { "Kabupaten Administrasi Kepulauan Seribu", "31.01" },
                
                // Nusa Tenggara Barat
                { "Lombok Barat", "52.01" },
                { "Lombok Tengah", "52.02" },
                { "Lombok Timur", "52.03" },
                { "Sumbawa", "52.04" },
                { "Dompu", "52.05" },
                { "Bima", "52.06" },
                { "Sumbawa Barat", "52.07" },
                { "Lombok Utara", "52.08" },
                { "Kota Mataram", "52.71" },
                { "Kota Bima", "52.72" },
                
                // Nusa Tenggara Timur
                { "Kupang", "5301" },
                { "Timor Tengah Selatan", "5302" },
                { "Timor Tengah Utara", "5303" },
                { "Belu", "5304" },
                { "Alor", "5305" },
                { "Flores Timur", "5306" },
                { "Sikka", "5307" },
                { "Ende", "5308" },
                { "Ngada", "5309" },
                { "Manggarai", "5310" },
                { "Sumba Timur", "5311" },
                { "Sumba Barat", "5312" },
                { "Lembata", "5313" },
                { "Rote Ndao", "5314" },
                { "Manggarai Barat", "5315" },
                { "Nagekeo", "5316" },
                { "Sumba Tengah", "5317" },
                { "Sumba Barat Daya", "5318" },
                { "Manggarai Timur", "5319" },
                { "Sabu Raijua", "5320" },
                { "Malaka", "5321" },
                { "Kota Kupang", "5371" },
                
                // Bali
                { "Jembrana", "5101" },
                { "Tabanan", "5102" },
                { "Badung", "5103" },
                { "Gianyar", "5104" },
                { "Karangasem", "5105" },
                { "Klungkung", "5106" },
                { "Bangli", "5107" },
                { "Buleleng", "5108" },
                { "Denpasar", "5171" },
                
                // Additional mappings for port cities
                { "Larantuka", "5306" }, // Flores Timur
                { "Kayangan", "52.08" }, // Lombok Utara
                { "Mataram", "52.71" }, // Kota Mataram
                { "Surabaya", "35.78" }, // Kota Surabaya
                { "Denpasar", "5171" }, // Kota Denpasar
                { "Lembar", "52.01" }, // Lombok Barat
                { "Tanjung Perak", "35.78" }, // Kota Surabaya
                { "Benoa", "5171" } // Kota Denpasar
            };

            var result = regencyMap.ContainsKey(regencyName) ? regencyMap[regencyName] : "";
            Console.WriteLine($"GetRegencyCodeByName - Result: '{result}' (Found: {regencyMap.ContainsKey(regencyName)})");
            return result;
        }

        #endregion
        // File: Controllers/PermitController.cs
        // Tambahkan API endpoints untuk mendukung edit mode

        #region API Endpoints for Edit Mode

        /// <summary>
        /// API untuk mendapatkan data lokasi lengkap berdasarkan string lokasi
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ParseLocationString(string locationString)
        {
            try
            {
                if (string.IsNullOrEmpty(locationString))
                {
                    return Json(new { success = false, message = "Location string is empty" });
                }

                // Parse format: "Kabupaten, Provinsi"
                var parts = locationString.Split(',');
                if (parts.Length >= 2)
                {
                    var regencyName = parts[0].Trim();
                    var provinceName = parts[1].Trim();

                    // You would implement actual lookup here
                    var result = new
                    {
                        success = true,
                        provinceCode = GetProvinceCodeByName(provinceName),
                        provinceName = provinceName,
                        regencyCode = GetRegencyCodeByName(regencyName),
                        regencyName = regencyName
                    };

                    return Json(result);
                }

                return Json(new { success = false, message = "Invalid location format" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API untuk validasi edit data sebelum submit
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ValidateEditData([FromBody] EditValidationRequest request)
        {
            try
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                // Validate basic fields
                if (string.IsNullOrWhiteSpace(request.CompanyName))
                    errors.Add("Nama perusahaan harus diisi");

                if (string.IsNullOrWhiteSpace(request.OriginLocation))
                    errors.Add("Lokasi asal harus diisi");

                if (string.IsNullOrWhiteSpace(request.DestinationLocation))
                    errors.Add("Lokasi tujuan harus diisi");

                // Validate livestock details
                if (request.LivestockDetails == null || !request.LivestockDetails.Any())
                {
                    errors.Add("Minimal harus ada satu detail ternak");
                }
                else
                {
                    var validLivestock = request.LivestockDetails.Where(l =>
                        !string.IsNullOrEmpty(l.LivestockType) && l.Quantity > 0).ToList();

                    if (!validLivestock.Any())
                    {
                        errors.Add("Minimal harus ada satu detail ternak yang valid");
                    }

                    // Validate quota if origin province is provided
                    if (!string.IsNullOrEmpty(request.OriginProvinceCode))
                    {
                        foreach (var livestock in validLivestock)
                        {
                            var quotaValidation = await ValidateQuotaForEdit(
                                livestock.LivestockType,
                                request.OriginProvinceCode,
                                livestock.Quantity);

                            if (!quotaValidation.IsValid)
                            {
                                errors.Add($"{livestock.LivestockType}: {quotaValidation.Message}");
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    isValid = errors.Count == 0,
                    errors = errors,
                    warnings = warnings
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Terjadi kesalahan validasi: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Helper method untuk validasi kuota dalam edit mode
        /// </summary>
        private async Task<(bool IsValid, string Message)> ValidateQuotaForEdit(string livestockType, string provinceCode, int quantity)
        {
            try
            {
                // Implement your quota validation logic here
                // This is a placeholder implementation

                // In real implementation, you would:
                // 1. Get quota data from database
                // 2. Check available quota
                // 3. Return validation result

                return (true, "Kuota tersedia");
            }
            catch (Exception ex)
            {
                return (false, $"Error validating quota: {ex.Message}");
            }
        }

        #endregion

        // File: Controllers/PermitController.cs
        // Hapus bagian PortController yang sementara dan update helper methods

        #region Updated Helper Methods for Port Integration

        /// <summary>
        /// Get province code from port name using existing Port database
        /// </summary>
        private async Task<string> GetProvinceCodeFromPortName(string portName)
        {
            try
            {
                if (string.IsNullOrEmpty(portName)) return "";

                var port = await _context.Ports
                    .Where(p => p.Name == portName && p.IsActive)
                    .FirstOrDefaultAsync();

                return port?.ProvinceCode ?? "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting province code from port: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Get regency code from port name using existing Port database
        /// </summary>
        private async Task<string> GetRegencyCodeFromPortName(string portName)
        {
            try
            {
                if (string.IsNullOrEmpty(portName)) return "";

                var port = await _context.Ports
                    .Where(p => p.Name == portName && p.IsActive)
                    .FirstOrDefaultAsync();

                if (port == null) return "";

                // Use the City property to get regency code
                var regencyCode = GetRegencyCodeByName(port.City);
                Console.WriteLine($"GetRegencyCodeFromPortName - Port: {portName}, City: {port.City}, Regency Code: {regencyCode}");
                
                return regencyCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting regency code from port: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Validate port selection based on origin/destination province
        /// </summary>
        private async Task<(bool IsValid, string Message)> ValidatePortSelection(string portName, string expectedProvinceCode)
        {
            try
            {
                if (string.IsNullOrEmpty(portName))
                    return (true, ""); // Allow empty port selection

                var port = await _context.Ports
                    .Where(p => p.Name == portName && p.IsActive)
                    .FirstOrDefaultAsync();

                if (port == null)
                {
                    return (false, $"Pelabuhan '{portName}' tidak ditemukan atau tidak aktif");
                }

                if (!string.IsNullOrEmpty(expectedProvinceCode) && port.ProvinceCode != expectedProvinceCode)
                {
                    return (false, $"Pelabuhan '{portName}' tidak sesuai dengan provinsi yang dipilih");
                }

                return (true, $"Pelabuhan '{portName}' valid");
            }
            catch (Exception ex)
            {
                return (false, $"Error validating port: {ex.Message}");
            }
        }

        /// <summary>
        /// Enhanced field validation including port validation
        /// </summary>
        private async Task<List<string>> ApplyBasicFieldChanges(LivestockPermitApplication permit, PermitApprovalViewModel model, Dictionary<string, string> originalData)
        {
            var changedFields = new List<string>();

            // Track original values
            originalData["CompanyName"] = permit.CompanyName;
            originalData["CompanyAddress"] = permit.CompanyAddress ?? "";
            originalData["OriginLocation"] = permit.OriginLocation ?? "";
            originalData["DestinationLocation"] = permit.DestinationLocation ?? "";
            originalData["DeparturePort"] = permit.DeparturePort ?? "";
            originalData["ArrivalPort"] = permit.ArrivalPort ?? "";

            // Apply basic field changes (same as before)
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

            // ⭐ UPDATED: Enhanced port validation and updates
            if (!string.IsNullOrEmpty(model.EditableDeparturePort) &&
                permit.DeparturePort != model.EditableDeparturePort.Trim())
            {
                var portValidation = await ValidatePortSelection(
                    model.EditableDeparturePort.Trim(),
                    ExtractProvinceCodeFromLocation(model.EditableOriginLocation)
                );

                if (!portValidation.IsValid)
                {
                    throw new InvalidOperationException($"Pelabuhan keberangkatan tidak valid: {portValidation.Message}");
                }

                permit.DeparturePort = model.EditableDeparturePort.Trim();
                changedFields.Add($"Pelabuhan Keberangkatan: '{originalData["DeparturePort"]}' → '{permit.DeparturePort}'");
            }

            if (!string.IsNullOrEmpty(model.EditableArrivalPort) &&
                permit.ArrivalPort != model.EditableArrivalPort.Trim())
            {
                var portValidation = await ValidatePortSelection(
                    model.EditableArrivalPort.Trim(),
                    ExtractProvinceCodeFromLocation(model.EditableDestinationLocation)
                );

                if (!portValidation.IsValid)
                {
                    throw new InvalidOperationException($"Pelabuhan tujuan tidak valid: {portValidation.Message}");
                }

                permit.ArrivalPort = model.EditableArrivalPort.Trim();
                changedFields.Add($"Pelabuhan Tiba: '{originalData["ArrivalPort"]}' → '{permit.ArrivalPort}'");
            }

            return changedFields;
        }

        /// <summary>
        /// Extract province code from location string with better logic
        /// </summary>
        private string ExtractProvinceCodeFromLocation(string location)
        {
            if (string.IsNullOrEmpty(location)) return "";

            // Enhanced province mapping
            var provinceMapping = new Dictionary<string, string>
    {
        { "Nusa Tenggara Barat", "52" },
        { "NTB", "52" },
        { "Nusa Tenggara Timur", "53" },
        { "NTT", "53" },
        { "Bali", "51" },
        { "Jawa Timur", "35" },
        { "Jatim", "35" },
        { "Jawa Tengah", "33" },
        { "Jateng", "33" },
        { "Jawa Barat", "32" },
        { "Jabar", "32" },
        { "DKI Jakarta", "31" },
        { "Jakarta", "31" },
        { "Sulawesi Selatan", "73" },
        { "Sulsel", "73" },
        { "Kalimantan Timur", "64" },
        { "Kaltim", "64" },
        { "Kalimantan Selatan", "63" },
        { "Kalsel", "63" },
        { "Sumatera Utara", "12" },
        { "Sumut", "12" },
        { "Lampung", "18" }
    };

            var parts = location.Split(',');
            if (parts.Length >= 2)
            {
                var province = parts.Last().Trim();

                // Try exact match first
                if (provinceMapping.ContainsKey(province))
                {
                    return provinceMapping[province];
                }

                // Try partial match
                foreach (var mapping in provinceMapping)
                {
                    if (province.Contains(mapping.Key) || mapping.Key.Contains(province))
                    {
                        return mapping.Value;
                    }
                }
            }

            return "52"; // Default to NTB
        }

        /// <summary>
        /// API endpoint untuk mendapatkan port suggestions saat editing
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPortSuggestions(string provinceCode, string term = "")
        {
            try
            {
                var query = _context.Ports
                    .Where(p => p.IsActive);

                // Filter by province if provided
                if (!string.IsNullOrEmpty(provinceCode))
                {
                    query = query.Where(p => p.ProvinceCode == provinceCode);
                }

                // Filter by search term if provided
                if (!string.IsNullOrEmpty(term))
                {
                    query = query.Where(p =>
                        p.Name.Contains(term) ||
                        p.City.Contains(term));
                }

                var ports = await query
                    .OrderBy(p => p.Name)
                    .Take(20)
                    .Select(p => new
                    {
                        id = p.Name,
                        text = $"{p.Name} ({p.City})",
                        city = p.City,
                        province = p.Province,
                        type = p.Type
                    })
                    .ToListAsync();

                return Json(new { results = ports });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting port suggestions: {ex.Message}");
                return Json(new { results = new List<object>() });
            }
        }

        #endregion

        #region Request Models for Edit Validation

        public class EditValidationRequest
        {
            public int PermitId { get; set; }
            public string CompanyName { get; set; }
            public string CompanyAddress { get; set; }
            public string OriginLocation { get; set; }
            public string DestinationLocation { get; set; }
            public string DeparturePort { get; set; }
            public string ArrivalPort { get; set; }
            public string OriginProvinceCode { get; set; }
            public List<EditLivestockRequest> LivestockDetails { get; set; } = new();
        }

        public class EditLivestockRequest
        {
            public string LivestockType { get; set; }
            public int Quantity { get; set; }
            public string Description { get; set; }
        }

        // ===============================================
        // DOCUMENT MANAGEMENT METHODS
        // ===============================================

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

                // Validate permit exists and belongs to user
                var permit = await _context.PermitApplications
                    .Include(p => p.Documents)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null)
                {
                    return Json(new { success = false, message = "Permohonan tidak ditemukan" });
                }

                // Find document
                var document = permit.Documents.FirstOrDefault(d => d.Id == documentId);
                if (document == null)
                {
                    return Json(new { success = false, message = "Dokumen tidak ditemukan" });
                }

                // Update document details
                document.DocumentType = documentType;
                
                // Define document types that don't need number and date
                var noNumberDateTypes = new[] { "SKKH_KABUPATEN_ASAL", "SKKH_DINAS_PROVINSI", "SURAT_JALAN_TERNAK", "HASIL_PEMERIKSAAN_FISIK" };
                
                // Set document number and date based on document type
                if (noNumberDateTypes.Contains(documentType))
                {
                    // For these document types, always set to null
                    document.DocumentNumber = null;
                    document.DocumentDate = null;
                }
                else
                {
                    // For other document types, use the provided values
                    document.DocumentNumber = string.IsNullOrEmpty(documentNumber) ? null : documentNumber;
                    
                    // Parse document date safely
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

                // Handle file upload if provided
                if (documentFile != null && documentFile.Length > 0)
                {
                    var validation = ValidateUploadedFile(documentFile);
                    if (!validation.IsValid)
                    {
                        return Json(new { success = false, message = string.Join(", ", validation.Errors) });
                    }

                    // Delete old file
                    if (!string.IsNullOrEmpty(document.FilePath) && System.IO.File.Exists(document.FilePath))
                    {
                        System.IO.File.Delete(document.FilePath);
                    }

                    // Save new file
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

                // Validate permit exists
                var permit = await _context.PermitApplications
                    .Include(p => p.Documents)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null)
                {
                    return Json(new { success = false, message = "Permohonan tidak ditemukan" });
                }

                // Validate file
                var validation = ValidateUploadedFile(documentFile);
                if (!validation.IsValid)
                {
                    return Json(new { success = false, message = string.Join(", ", validation.Errors) });
                }

                // Check if document type already exists
                if (permit.Documents.Any(d => d.DocumentType == documentType))
                {
                    return Json(new { success = false, message = $"Dokumen jenis {documentType} sudah ada" });
                }

                // Save file
                var fileName = SanitizeFileName(documentFile.FileName);
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var uniqueFileName = $"{documentType}_{timestamp}_{fileName}";
                var filePath = Path.Combine(_environment.WebRootPath, "documents", "supporting", uniqueFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await documentFile.CopyToAsync(stream);
                }

                // Define document types that don't need number and date
                var noNumberDateTypes = new[] { "SKKH_KABUPATEN_ASAL", "SKKH_DINAS_PROVINSI", "SURAT_JALAN_TERNAK", "HASIL_PEMERIKSAAN_FISIK" };
                
                // Create document record
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

                // Validate permit exists
                var permit = await _context.PermitApplications
                    .Include(p => p.Documents)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null)
                {
                    return Json(new { success = false, message = "Permohonan tidak ditemukan" });
                }

                // Find document
                var document = permit.Documents.FirstOrDefault(d => d.Id == documentId);
                if (document == null)
                {
                    return Json(new { success = false, message = "Dokumen tidak ditemukan" });
                }

                // Delete file
                if (!string.IsNullOrEmpty(document.FilePath))
                {
                    var fullPath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                // Remove from database
                _context.PermitDocuments.Remove(document);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Dokumen berhasil dihapus" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        private async Task<string> GetCurrentUserName()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return "Unknown";

            var user = await _context.Users.FindAsync(userId.Value);
            return user?.NamaLengkap ?? "Unknown";
        }

        private string GetDocumentDisplayName(string documentType)
        {
            var displayNames = new Dictionary<string, string>
            {
                { "SURAT_PERMOHONAN", "Surat Permohonan" },
                { "REKOMENDASI_DINAS_PROV", "Rekomendasi Dinas Peternakan Provinsi NTB" },
                { "REKOMENDASI_DAERAH_TUJUAN", "Rekomendasi Pemasukan Ternak dari Daerah Tujuan" },
                { "SKKH_KABUPATEN_ASAL", "SKKH dari Kabupaten Asal" },
                { "SKKH_DINAS_PROVINSI", "SKKH dari Dinas Peternakan Provinsi NTB" },
                { "SURAT_JALAN_TERNAK", "Surat Keterangan Jalan Ternak/Rekomendasi Asal" },
                { "HASIL_PEMERIKSAAN_FISIK", "Hasil Pemeriksaan Fisik (Holding Ground)" },
                { "DOKUMEN_OPSIONAL", "Dokumen Opsional" }
            };

            return displayNames.ContainsKey(documentType) ? displayNames[documentType] : documentType;
        }

        #endregion

        // ===============================================
        // USER ROLE MANAGEMENT METHODS
        // ===============================================

        [HttpGet]
        public async Task<IActionResult> UserManagement()
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
                var users = await _context.Users
                    .Select(u => new UserManagementViewModel
                    {
                        Id = u.Id,
                        NamaLengkap = u.NamaLengkap,
                        Email = u.Email,
                        NoTelepon = u.NoTelepon,
                        Role = u.Role,
                        IsActive = u.IsActive,
                        CreatedAt = u.TanggalDaftar,
                        LastLoginAt = null // User model doesn't have LastLoginAt
                    })
                    .OrderBy(u => u.NamaLengkap)
                    .ToListAsync();

                return View(users);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Terjadi kesalahan: {ex.Message}";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Json(new { success = false, message = "User tidak ditemukan" });

                var userRole = HttpContext.Session.GetString("Role");
                if (userRole != "Admin")
                {
                    return Json(new { success = false, message = "Akses ditolak" });
                }

                // Debug logging
                Console.WriteLine($"GetUserDetails called for user ID: {id}");
                Console.WriteLine($"Current user ID: {userId}");
                Console.WriteLine($"Current user role: {userRole}");

                var user = await _context.Users
                    .Where(u => u.Id == id)
                    .Select(u => new
                    {
                        u.Id,
                        u.NamaLengkap,
                        u.Email,
                        u.NoTelepon,
                        u.Role,
                        u.IsActive,
                        RegistrationDate = u.TanggalDaftar,
                        LastLoginDate = (DateTime?)null,
                        Alamat = u.Alamat ?? "-"
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    Console.WriteLine($"User with ID {id} not found");
                    return Json(new { success = false, message = "User tidak ditemukan" });
                }

                // Debug logging
                Console.WriteLine($"User found: {user.NamaLengkap}, Email: {user.Email}, Role: {user.Role}");

                return Json(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserDetails: {ex.Message}");
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(int userId, string newRole)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null) return Json(new { success = false, message = "User tidak ditemukan" });

                var currentUserRole = HttpContext.Session.GetString("Role");
                if (currentUserRole != "Admin")
                {
                    return Json(new { success = false, message = "Akses ditolak. Hanya Admin yang dapat mengubah role." });
                }

                // Validasi role yang valid
                var validRoles = new[] { "User", "Admin", "Verifikator", "KepalaDinas" };
                if (!validRoles.Contains(newRole))
                {
                    return Json(new { success = false, message = "Role tidak valid" });
                }

                // Cek apakah user mencoba mengubah role sendiri
                if (userId == currentUserId)
                {
                    return Json(new { success = false, message = "Tidak dapat mengubah role sendiri" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User tidak ditemukan" });
                }

                var oldRole = user.Role;
                user.Role = newRole;

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Role user {user.NamaLengkap} berhasil diubah dari {oldRole} menjadi {newRole}" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null) return Json(new { success = false, message = "User tidak ditemukan" });

                var currentUserRole = HttpContext.Session.GetString("Role");
                if (currentUserRole != "Admin")
                {
                    return Json(new { success = false, message = "Akses ditolak. Hanya Admin yang dapat mengubah status user." });
                }

                // Cek apakah user mencoba mengubah status sendiri
             
                if (userId == currentUserId)
                {
                    return Json(new { success = false, message = "Tidak dapat mengubah status sendiri" });
                }


                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User tidak ditemukan" });
                }

                var oldStatus = user.IsActive;
                user.IsActive = !user.IsActive;

                await _context.SaveChangesAsync();

                var statusText = user.IsActive ? "aktif" : "nonaktif";
                return Json(new { 
                    success = true, 
                    message = $"Status user {user.NamaLengkap} berhasil diubah menjadi {statusText}",
                    newStatus = user.IsActive
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserStatistics()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Json(new { success = false, message = "User tidak ditemukan" });

                var userRole = HttpContext.Session.GetString("Role");
                if (userRole != "Admin")
                {
                    return Json(new { success = false, message = "Akses ditolak" });
                }

                var statistics = await _context.Users
                    .GroupBy(u => u.Role)
                    .Select(g => new
                    {
                        Role = g.Key,
                        Count = g.Count(),
                        ActiveCount = g.Count(u => u.IsActive),
                        InactiveCount = g.Count(u => !u.IsActive)
                    })
                    .ToListAsync();

                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                var inactiveUsers = await _context.Users.CountAsync(u => !u.IsActive);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        RoleStatistics = statistics,
                        TotalUsers = totalUsers,
                        ActiveUsers = activeUsers,
                        InactiveUsers = inactiveUsers
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestUserData()
        {
            try
            {
                var users = await _context.Users.Take(5).ToListAsync();
                var userData = users.Select(u => new
                {
                    u.Id,
                    u.NamaLengkap,
                    u.Email,
                    u.NoTelepon,
                    u.Role,
                    u.IsActive,
                    u.TanggalDaftar,
                    u.Alamat
                }).ToList();

                return Json(new { success = true, data = userData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult TestUserDataPage()
        {
            return View("TestUserData");
        }

        [HttpGet]
        public IActionResult TestUserStatistics()
        {
            // Test method untuk debugging
            var userId = GetCurrentUserId();
            var userRole = HttpContext.Session.GetString("Role");
            
            Console.WriteLine($"TestUserStatistics - UserId: {userId}");
            Console.WriteLine($"TestUserStatistics - UserRole: {userRole}");
            
            return Json(new { 
                success = true, 
                userId = userId, 
                userRole = userRole,
                message = "Test method berhasil diakses"
            });
        }

        [HttpGet]
        public async Task<IActionResult> UserStatistics()
        {
            try
            {
                Console.WriteLine("UserStatistics method called");
                
                var userId = GetCurrentUserId();
                Console.WriteLine($"UserStatistics - UserId: {userId}");
                
                if (userId == null) 
                {
                    Console.WriteLine("UserStatistics - UserId is null, redirecting to Login");
                    return RedirectToAction("Login", "Auth");
                }

                var userRole = HttpContext.Session.GetString("Role");
                Console.WriteLine($"UserStatistics - UserRole: {userRole}");
                
                // Temporary: Allow access for debugging
                if (userRole != "Admin")
                {
                    Console.WriteLine($"UserStatistics - UserRole '{userRole}' is not Admin, but allowing access for debugging");
                    // return RedirectToAction("Index", "Dashboard");
                }

                // Get user statistics
                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                var inactiveUsers = await _context.Users.CountAsync(u => !u.IsActive);

                var roleStatisticsData = await _context.Users
                    .GroupBy(u => u.Role)
                    .Select(g => new
                    {
                        Role = g.Key,
                        Count = g.Count(),
                        ActiveCount = g.Count(u => u.IsActive),
                        InactiveCount = g.Count(u => !u.IsActive)
                    })
                    .ToListAsync();

                var monthlyRegistrationsData = await _context.Users
                    .Where(u => u.TanggalDaftar >= DateTime.Now.AddMonths(-6))
                    .GroupBy(u => new { u.TanggalDaftar.Year, u.TanggalDaftar.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                var formattedMonthlyData = monthlyRegistrationsData.Select(g => new
                {
                    Month = $"{g.Year}-{g.Month:D2}",
                    Count = g.Count
                }).ToList();

                var recentUsersData = await _context.Users
                    .OrderByDescending(u => u.TanggalDaftar)
                    .Take(10)
                    .Select(u => new
                    {
                        u.Id,
                        u.NamaLengkap,
                        u.Email,
                        u.Role,
                        u.IsActive,
                        u.TanggalDaftar
                    })
                    .ToListAsync();

                var viewModel = new UserStatisticsViewModel
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    InactiveUsers = inactiveUsers,
                    RoleStatistics = roleStatisticsData.Select(r => new RoleStatistics
                    {
                        Role = r.Role,
                        Count = r.Count,
                        ActiveCount = r.ActiveCount,
                        InactiveCount = r.InactiveCount
                    }).ToList(),
                    MonthlyRegistrations = formattedMonthlyData.Select(m => new MonthlyRegistration
                    {
                        Month = m.Month,
                        Count = m.Count
                    }).ToList(),
                    RecentUsers = recentUsersData.Select(ru => new RecentUser
                    {
                        Id = ru.Id,
                        NamaLengkap = ru.NamaLengkap,
                        Email = ru.Email,
                        Role = ru.Role,
                        IsActive = ru.IsActive,
                        TanggalDaftar = ru.TanggalDaftar
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UserStatistics - Exception: {ex.Message}");
                TempData["ErrorMessage"] = $"Terjadi kesalahan: {ex.Message}";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PreviewPdf(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Hanya Admin yang dapat melihat preview PDF";
                return RedirectToAction("Index");
            }

            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .Include(p => p.Documents)
                .Include(p => p.Admin)
                .Include(p => p.Verifikator)
                .Include(p => p.KepalaDinas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return RedirectToAction("Index");
            }

            try
            {
                // Generate PDF content using the existing service
                var pdfBytes = await _pdfGenerator.GeneratePermitPdf(permit);
                var pdfContent = System.Text.Encoding.UTF8.GetString(pdfBytes);

                return Content(pdfContent, "text/html");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal generate preview PDF. Silakan coba lagi.";
                return RedirectToAction("Approve", new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PreviewPdfEditMode(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Hanya Admin yang dapat melihat preview PDF";
                return RedirectToAction("Index");
            }

            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .Include(p => p.Documents)
                .Include(p => p.Admin)
                .Include(p => p.Verifikator)
                .Include(p => p.KepalaDinas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return RedirectToAction("Index");
            }

            try
            {
                // Generate PDF content using the existing service
                var pdfBytes = await _pdfGenerator.GeneratePermitPdf(permit);
                var pdfContent = System.Text.Encoding.UTF8.GetString(pdfBytes);

                return Content(pdfContent, "text/html");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal generate preview PDF. Silakan coba lagi.";
                return RedirectToAction("EnableEditMode", new { id });
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
                return RedirectToAction("Index");
            }

            try
            {
                var permit = await _context.PermitApplications
                    .Include(p => p.LivestockDetails)
                    .FirstOrDefaultAsync(p => p.Id == model.Id);

                if (permit == null)
                {
                    TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                    return RedirectToAction("Index");
                }

                // Apply changes without approval
                var changedFields = new List<string>();

                // Apply basic field changes
                var basicChanges = await ApplyBasicFieldChanges(permit, model, new Dictionary<string, string>());
                changedFields.AddRange(basicChanges);

                // Apply livestock changes
                var livestockChanges = await ApplyLivestockChanges(permit, model);
                changedFields.AddRange(livestockChanges);

                // Save changes
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

        [HttpGet]
        public async Task<IActionResult> TestApplicationNumber()
        {
            try
            {
                var applicationNumber = await _applicationNumberService.GenerateApplicationNumberAsync();
                return Json(new { success = true, applicationNumber = applicationNumber });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckDatabaseData()
        {
            try
            {
                var year = DateTime.Now.Year;
                var month = DateTime.Now.Month;
                
                var existingApplications = await _context.PermitApplications
                    .Where(p => p.SubmissionDate.Year == year && p.SubmissionDate.Month == month)
                    .Select(p => new { p.ApplicationNumber, p.SubmissionDate })
                    .ToListAsync();

                var allApplications = await _context.PermitApplications
                    .OrderByDescending(p => p.SubmissionDate)
                    .Take(10)
                    .Select(p => new { p.ApplicationNumber, p.SubmissionDate })
                    .ToListAsync();

                var totalCount = await _context.PermitApplications.CountAsync();
                
                return Json(new { 
                    success = true, 
                    year = year,
                    month = month,
                    existingApplications = existingApplications,
                    allApplications = allApplications,
                    totalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message, stackTrace = ex.StackTrace });
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
                
                // Return file for preview (browser will handle display)
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat dokumen";
                return NotFound();
            }
        }

        /// <summary>
        /// Download dokumen izin dalam format HTML yang bisa dicetak
        /// </summary>
        /// <param name="id">ID permohonan izin</param>
        /// <returns>File HTML untuk diunduh</returns>
        [HttpGet]
        public async Task<IActionResult> DownloadPdfFile(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");

            // Ambil data permohonan
            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .Include(p => p.Admin)
                .Include(p => p.Verifikator)
                .Include(p => p.KepalaDinas)
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return NotFound();
            }

            // Cek akses berdasarkan role
            bool canDownload = false;
            switch (userRole)
            {
                case "User":
                    canDownload = permit.UserId == userId.Value && permit.Status >= PermitStatus.AdminApproved;
                    break;
                case "Admin":
                    canDownload = permit.Status >= PermitStatus.AdminApproved;
                    break;
                case "Verifikator":
                    canDownload = permit.Status >= PermitStatus.AdminApproved;
                    break;
                case "KepalaDinas":
                    canDownload = permit.Status >= PermitStatus.VerifikatorApproved;
                    break;
            }

            if (!canDownload)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengunduh dokumen ini";
                return Forbid();
            }

            try
            {
                // Generate nama file
                var fileName = $"permit_{permit.ApplicationNumber}_{DateTime.Now:yyyyMMddHHmmss}.html";
                
                // Generate HTML content
                var htmlContent = await _pdfGenerator.GeneratePermitPdf(permit);
                
                if (htmlContent == null || htmlContent.Length == 0)
                {
                    TempData["ErrorMessage"] = "Gagal generate konten dokumen";
                    return RedirectToAction("Detail", new { id = id });
                }

                // Return file untuk download sebagai HTML
                return File(htmlContent, "text/html", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mengunduh dokumen";
                return RedirectToAction("Detail", new { id = id });
            }
        }


    }
}