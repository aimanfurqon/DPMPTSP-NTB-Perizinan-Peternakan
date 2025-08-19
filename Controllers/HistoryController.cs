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
    public class HistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HistoryController(ApplicationDbContext context)
        {
            _context = context;
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

        #region Private Methods

        private int? GetCurrentUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }

        #endregion
    }
}
