using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.Services;
using PerizinanPeternakan.ViewModels;

namespace PerizinanPeternakan.Controllers
{
    public class PermitController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfGeneratorService _pdfGenerator;
        private readonly IWebHostEnvironment _environment;

        public PermitController(ApplicationDbContext context, IPdfGeneratorService pdfGenerator, IWebHostEnvironment environment)
        {
            _context = context;
            _pdfGenerator = pdfGenerator;
            _environment = environment;
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

                // Query dasar untuk admin history
                var query = _context.PermitApplications
                    .Where(p => p.AdminId == userId.Value &&
                               (p.Status >= PermitStatus.AdminApproved || p.Status == PermitStatus.AdminRejected))
                    .Include(p => p.User)
                    .Include(p => p.ApprovalHistory.Where(h => h.UserId == userId.Value))
                    .Include(p => p.Documents)
                    .AsQueryable();

                // Apply filters
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

                // Get total count for pagination
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                // Get paged data
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

                // Get statistics
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

                // Pagination info
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < totalPages;

                // Filter values untuk form
                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
                ViewBag.StatusFilter = statusFilter;
                ViewBag.SearchTerm = searchTerm;

                // HAPUS BAGIAN INI - tidak diperlukan lagi:
                // ViewBag.StatusOptions = new SelectList(...);

                return View(adminHistoryList);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat riwayat review.";
                return View(new List<AdminHistoryViewModel>());
            }
        }

        // GET: Permit/TestAddAdminHistory - Form untuk testing manual add (hanya untuk development)
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

            // Get permits yang bisa ditambahkan history
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

        // POST: Permit/TestAddAdminHistory - Submit manual add
        [HttpPost]
        public async Task<IActionResult> TestAddAdminHistory(int permitId, string action, string comments)
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
                    TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                    return RedirectToAction("TestAddAdminHistory");
                }

                // Cek apakah sudah ada history admin untuk permit ini dari user yang sama
                var existingHistory = await _context.PermitApprovalHistories
                    .AnyAsync(h => h.PermitApplicationId == permitId &&
                                  h.UserId == userId.Value &&
                                  h.Action.Contains("Admin"));

                if (existingHistory)
                {
                    TempData["ErrorMessage"] = "Anda sudah memberikan review untuk permohonan ini";
                    return RedirectToAction("TestAddAdminHistory");
                }

                var fromStatus = permit.Status;
                var toStatus = action == "approve" ? PermitStatus.AdminApproved : PermitStatus.AdminRejected;
                var actionText = action == "approve" ? "Disetujui Admin" : "Ditolak Admin";

                var history = new PermitApprovalHistory
                {
                    PermitApplicationId = permitId,
                    UserId = userId.Value,
                    FromStatus = fromStatus,
                    ToStatus = toStatus,
                    Action = actionText,
                    Comments = comments ?? "Review admin manual",
                    ActionDate = DateTime.Now
                };

                _context.PermitApprovalHistories.Add(history);

                // Update permit
                permit.Status = toStatus;
                permit.AdminId = userId.Value;
                permit.AdminApprovalDate = DateTime.Now;

                if (action == "reject")
                {
                    permit.RejectionReason = comments;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"History admin berhasil ditambahkan untuk {permit.ApplicationNumber}";
                return RedirectToAction("AdminHistory");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat menambahkan history";
                return RedirectToAction("TestAddAdminHistory");
            }
        }

        // GET: Permit/BulkSeedAdminHistory - Seed bulk data untuk testing
        public async Task<IActionResult> BulkSeedAdminHistory()
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
                // Menggunakan seeder untuk bulk add
                AdminHistorySeeder.SeedAdminHistoryData(_context);
                AdminHistorySeeder.GenerateTestingAdminHistory(_context);

                TempData["SuccessMessage"] = "Bulk admin history data berhasil ditambahkan";
                return RedirectToAction("AdminHistory");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal menambahkan bulk data: " + ex.Message;
                return RedirectToAction("AdminHistory");
            }
        }

        // API endpoint untuk mendapatkan data admin history (untuk chart/dashboard)
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

        // Helper method untuk mendapatkan ringkasan aktivitas admin
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

        // POST: Permit/AddAdminHistory - Menambahkan history admin baru (untuk testing/manual entry)
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

                // Update permit
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

        // GET: Permit/AdminStatistics - API untuk statistik admin (untuk AJAX calls)
        public async Task<IActionResult> AdminStatistics()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin") return Forbid();

            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                var stats = new
                {
                    totalReviewed = await _context.PermitApplications.CountAsync(p => p.AdminId == userId.Value),
                    totalApproved = await _context.PermitApplications.CountAsync(p =>
                        p.AdminId == userId.Value &&
                        (p.Status == PermitStatus.AdminApproved ||
                         p.Status == PermitStatus.VerifikatorApproved ||
                         p.Status == PermitStatus.FinalApproved)),
                    totalRejected = await _context.PermitApplications.CountAsync(p =>
                        p.AdminId == userId.Value && p.Status == PermitStatus.AdminRejected),
                    thisMonthReviewed = await _context.PermitApplications.CountAsync(p =>
                        p.AdminId == userId.Value &&
                        p.AdminApprovalDate.HasValue &&
                        p.AdminApprovalDate.Value.Month == currentMonth &&
                        p.AdminApprovalDate.Value.Year == currentYear)
                };

                return Json(stats);
            }
            catch (Exception)
            {
                return Json(new { error = "Failed to load statistics" });
            }
        }

        //public async Task<IActionResult> AdminHistory()
        //{
        //    var userId = GetCurrentUserId();
        //    if (userId == null) return RedirectToAction("Login", "Auth");

        //    var userRole = HttpContext.Session.GetString("Role");
        //    if (userRole != "Admin")
        //    {
        //        TempData["ErrorMessage"] = "Akses ditolak. Halaman ini hanya untuk Admin.";
        //        return RedirectToAction("Index", "Dashboard");
        //    }

        //    try
        //    {
        //        // Ambil semua permohonan yang pernah di-review oleh admin
        //        var adminHistoryList = await _context.PermitApplications
        //            .Where(p => p.Status >= PermitStatus.AdminApproved || p.Status == PermitStatus.AdminRejected)
        //            .Include(p => p.User)
        //            .Include(p => p.ApprovalHistory.Where(h => h.UserId == userId.Value))
        //            .Include(p => p.Documents)
        //            .OrderByDescending(p => p.AdminApprovalDate ?? p.SubmissionDate)
        //            .Select(p => new AdminHistoryViewModel
        //            {
        //                Id = p.Id,
        //                ApplicationNumber = p.ApplicationNumber,
        //                CompanyName = p.CompanyName,
        //                ApplicantName = p.User.NamaLengkap,
        //                Status = p.Status,
        //                SubmissionDate = p.SubmissionDate,
        //                AdminApprovalDate = p.AdminApprovalDate,
        //                AdminComments = p.ApprovalHistory
        //                    .Where(h => h.UserId == userId.Value && h.Action.Contains("Admin"))
        //                    .OrderByDescending(h => h.ActionDate)
        //                    .Select(h => h.Comments)
        //                    .FirstOrDefault(),
        //                AdminAction = p.ApprovalHistory
        //                    .Where(h => h.UserId == userId.Value && h.Action.Contains("Admin"))
        //                    .OrderByDescending(h => h.ActionDate)
        //                    .Select(h => h.Action)
        //                    .FirstOrDefault(),
        //                DocumentCount = p.Documents.Count,
        //                CanView = true
        //            })
        //            .ToListAsync();

        //        ViewBag.AdminName = HttpContext.Session.GetString("NamaLengkap");
        //        ViewBag.TotalReviewed = adminHistoryList.Count;
        //        ViewBag.TotalApproved = adminHistoryList.Count(h => h.Status == PermitStatus.AdminApproved ||
        //                                                           h.Status == PermitStatus.VerifikatorApproved ||
        //                                                           h.Status == PermitStatus.FinalApproved);
        //        ViewBag.TotalRejected = adminHistoryList.Count(h => h.Status == PermitStatus.AdminRejected);

        //        return View(adminHistoryList);
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat riwayat review.";
        //        return View(new List<AdminHistoryViewModel>());
        //    }
        //}

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            var permits = new List<PermitListViewModel>();

            if (userRole == "User")
            {
                // User hanya melihat permohonan miliknya
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
                // Admin melihat permohonan yang perlu review data (level 1)
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
                // Verifikator melihat dokumen PDF yang sudah digenerate oleh admin (level 2)
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
                // Kepala Dinas melihat dokumen yang sudah diverifikasi (level 3)
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

        // GET: Permit/Create - Form permohonan baru
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
            model.LivestockDetails.Add(new LivestockDetailViewModel()); // Default 1 item
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

            // ============= COMPREHENSIVE DEBUGGING =============
            Console.WriteLine("=== COMPREHENSIVE FILE UPLOAD DEBUG ===");

            // 1. Debug Raw Request
            Console.WriteLine($"Request Content Type: {Request.ContentType}");
            Console.WriteLine($"Request Content Length: {Request.ContentLength}");
            Console.WriteLine($"Request Method: {Request.Method}");

            // 2. Debug Form Data
            Console.WriteLine($"Form Keys Count: {Request.Form.Keys.Count}");
            foreach (var key in Request.Form.Keys)
            {
                var values = Request.Form[key];
                Console.WriteLine($"Form Key: {key} = {string.Join(", ", values)}");
            }

            // 3. Debug Form Files
            Console.WriteLine($"Form Files Count: {Request.Form.Files.Count}");
            foreach (var file in Request.Form.Files)
            {
                Console.WriteLine($"Raw File: Name='{file.Name}', FileName='{file.FileName}', Length={file.Length}, ContentType='{file.ContentType}'");
            }

            // 4. Debug Model Properties (Before any processing)
            Console.WriteLine("=== MODEL PROPERTIES DEBUG ===");
            Console.WriteLine($"CompanyName: '{model.CompanyName}'");
            Console.WriteLine($"CompanyAddress: '{model.CompanyAddress}'");

            // 5. Debug File Properties in Model
            Console.WriteLine("=== MODEL FILE PROPERTIES DEBUG ===");
            var fileProperties = new[]
            {
        (nameof(model.SuratPermohonan), model.SuratPermohonan),
        (nameof(model.RekomendasiDinasProv), model.RekomendasiDinasProv),
        (nameof(model.RekomendasiDaerahTujuan), model.RekomendasiDaerahTujuan),
        (nameof(model.SKKHKabupatenAsal), model.SKKHKabupatenAsal),
        (nameof(model.SKKHDinasProvinsi), model.SKKHDinasProvinsi),
        (nameof(model.SuratJalanTernak), model.SuratJalanTernak),
        (nameof(model.HasilPemeriksaanFisik), model.HasilPemeriksaanFisik)
    };

            foreach (var (propName, file) in fileProperties)
            {
                if (file != null)
                {
                    Console.WriteLine($"✅ {propName}: {file.FileName} ({file.Length} bytes, {file.ContentType})");
                }
                else
                {
                    Console.WriteLine($"❌ {propName}: NULL");
                }
            }

            // 6. Manual File Binding (FALLBACK)
            if (Request.Form.Files.Count > 0 && fileProperties.All(f => f.Item2 == null))
            {
                Console.WriteLine("🔧 ATTEMPTING MANUAL FILE BINDING...");

                foreach (var formFile in Request.Form.Files)
                {
                    Console.WriteLine($"Processing form file: {formFile.Name}");

                    // Manual binding dengan case-insensitive matching
                    var propertyName = formFile.Name.ToLowerInvariant();
                    switch (propertyName)
                    {
                        case "suratpermohonan":
                            model.SuratPermohonan = formFile;
                            Console.WriteLine($"✅ Manually bound: SuratPermohonan");
                            break;
                        case "rekomendasidinasp rov":
                        case "rekomendasidinasprovansi":
                            model.RekomendasiDinasProv = formFile;
                            Console.WriteLine($"✅ Manually bound: RekomendasiDinasProv");
                            break;
                        case "rekomendasidaerahtujuan":
                            model.RekomendasiDaerahTujuan = formFile;
                            Console.WriteLine($"✅ Manually bound: RekomendasiDaerahTujuan");
                            break;
                        case "skkhkabupatensal":
                        case "skkh kabupaten asal":
                            model.SKKHKabupatenAsal = formFile;
                            Console.WriteLine($"✅ Manually bound: SKKHKabupatenAsal");
                            break;
                        case "skkhdinasprovinsi":
                            model.SKKHDinasProvinsi = formFile;
                            Console.WriteLine($"✅ Manually bound: SKKHDinasProvinsi");
                            break;
                        case "suratjalaternak":
                            model.SuratJalanTernak = formFile;
                            Console.WriteLine($"✅ Manually bound: SuratJalanTernak");
                            break;
                        case "hasilpemeriksaanfisik":
                            model.HasilPemeriksaanFisik = formFile;
                            Console.WriteLine($"✅ Manually bound: HasilPemeriksaanFisik");
                            break;
                        default:
                            Console.WriteLine($"❓ Unknown file property: {formFile.Name}");
                            break;
                    }
                }

                // Debug hasil manual binding
                Console.WriteLine("=== AFTER MANUAL BINDING ===");
                foreach (var (propName, file) in fileProperties)
                {
                    Console.WriteLine($"{propName}: {(file != null ? "BOUND" : "NULL")}");
                }
            }

            Console.WriteLine("=== END COMPREHENSIVE DEBUG ===");
            // ============= END DEBUGGING =============

            // Validate livestock details - minimal harus ada 1 yang valid
            if (model.LivestockDetails == null || !model.LivestockDetails.Any(d => !string.IsNullOrEmpty(d.LivestockType) && d.Quantity > 0))
            {
                ModelState.AddModelError("", "Minimal harus ada satu detail ternak yang valid");
            }

            // Enhanced document validation
            var documentValidationResult = ValidateRequiredDocuments(model);
            if (!documentValidationResult.IsValid)
            {
                foreach (var error in documentValidationResult.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }

            if (!ModelState.IsValid)
            {
                // Pastikan ada minimal 1 livestock detail untuk form
                if (model.LivestockDetails == null || !model.LivestockDetails.Any())
                {
                    model.LivestockDetails.Add(new LivestockDetailViewModel());
                }
                return View(model);
            }

            try
            {
                var applicationNumber = await GenerateApplicationNumber();

                var permitApplication = new LivestockPermitApplication
                {
                    ApplicationNumber = applicationNumber,
                    UserId = userId.Value,
                    CompanyName = model.CompanyName,
                    CompanyAddress = model.CompanyAddress,
                    OriginLocation = model.OriginLocation,
                    DestinationLocation = model.DestinationLocation,
                    DeparturePort = model.DeparturePort,
                    ArrivalPort = model.ArrivalPort,
                    Status = PermitStatus.Submitted,
                    SubmissionDate = DateTime.Now,
                    CurrentApprovalLevel = 1
                };

                _context.PermitApplications.Add(permitApplication);
                await _context.SaveChangesAsync();

                // Add livestock details
                foreach (var detail in model.LivestockDetails.Where(d => !string.IsNullOrEmpty(d.LivestockType) && d.Quantity > 0))
                {
                    var livestockDetail = new LivestockDetail
                    {
                        PermitApplicationId = permitApplication.Id,
                        LivestockType = detail.LivestockType,
                        Quantity = detail.Quantity,
                        Description = detail.Description
                    };
                    _context.LivestockDetails.Add(livestockDetail);
                }

                // Upload supporting documents with enhanced validation
                var uploadResult = await UploadSupportingDocuments(permitApplication.Id, model, userId.Value);
                if (!uploadResult.Success)
                {
                    // Rollback jika upload gagal
                    _context.PermitApplications.Remove(permitApplication);
                    await _context.SaveChangesAsync();

                    ModelState.AddModelError("", uploadResult.ErrorMessage);
                    return View(model);
                }

                // Add approval history
                var history = new PermitApprovalHistory
                {
                    PermitApplicationId = permitApplication.Id,
                    UserId = userId.Value,
                    FromStatus = PermitStatus.Draft,
                    ToStatus = PermitStatus.Submitted,
                    Action = "Submit",
                    Comments = $"Permohonan diajukan dengan {uploadResult.UploadedCount} dokumen pendukung",
                    ActionDate = DateTime.Now
                };
                _context.PermitApprovalHistories.Add(history);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Permohonan berhasil diajukan dengan nomor: {applicationNumber}. {uploadResult.UploadedCount} dokumen pendukung telah diupload.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating permit: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                ModelState.AddModelError("", "Terjadi kesalahan saat menyimpan permohonan. Silakan coba lagi.");

                // Pastikan ada minimal 1 livestock detail untuk form
                if (model.LivestockDetails == null || !model.LivestockDetails.Any())
                {
                    model.LivestockDetails.Add(new LivestockDetailViewModel());
                }

                return View(model);
            }
        }


        // REPLACE METHOD UploadSupportingDocuments DI PermitController.cs

        private async Task<(bool Success, string ErrorMessage, int UploadedCount)> UploadSupportingDocuments(int permitId, PermitApplicationViewModel model, int userId)
        {
            try
            {
                var documentsToUpload = new[]
                {
            (model.SuratPermohonan, "SURAT_PERMOHONAN", "Surat Permohonan"),
            (model.RekomendasiDinasProv, "REKOMENDASI_DINAS_PROV", "Rekomendasi Dinas Peternakan Provinsi NTB"),
            (model.RekomendasiDaerahTujuan, "REKOMENDASI_DAERAH_TUJUAN", "Rekomendasi Pemasukan Ternak dari Daerah Tujuan"),
            (model.SKKHKabupatenAsal, "SKKH_KABUPATEN_ASAL", "SKKH dari Kabupaten Asal"),
            (model.SKKHDinasProvinsi, "SKKH_DINAS_PROVINSI", "SKKH dari Dinas Peternakan Provinsi NTB"),
            (model.SuratJalanTernak, "SURAT_JALAN_TERNAK", "Surat Keterangan Jalan Ternak/Rekomendasi Asal"),
            (model.HasilPemeriksaanFisik, "HASIL_PEMERIKSAAN_FISIK", "Hasil Pemeriksaan Fisik (Holding Ground)")
        };

                var uploadsPath = Path.Combine(_environment.WebRootPath, "documents", "supporting");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                int uploadedCount = 0;
                var uploadedDocuments = new List<PermitDocument>();

                foreach (var (file, documentType, documentName) in documentsToUpload)
                {
                    if (file != null && file.Length > 0)
                    {
                        // Validate file before processing
                        var fileValidation = ValidateUploadedFile(file);
                        if (!fileValidation.IsValid)
                        {
                            return (false, $"Error pada {documentName}: {string.Join(", ", fileValidation.Errors)}", 0);
                        }

                        try
                        {
                            // Generate unique filename dengan safety checks
                            var fileExtension = Path.GetExtension(file.FileName).ToLower();
                            var sanitizedFileName = SanitizeFileName(file.FileName);
                            var uniqueFileName = $"{permitId}_{documentType}_{DateTime.Now:yyyyMMddHHmmss}_{uploadedCount}{fileExtension}";
                            var filePath = Path.Combine(uploadsPath, uniqueFileName);

                            // Ensure file doesn't already exist
                            while (System.IO.File.Exists(filePath))
                            {
                                uniqueFileName = $"{permitId}_{documentType}_{DateTime.Now:yyyyMMddHHmmssfff}_{uploadedCount}{fileExtension}";
                                filePath = Path.Combine(uploadsPath, uniqueFileName);
                            }

                            // Save file dengan error handling
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            // Verify file was saved correctly
                            if (!System.IO.File.Exists(filePath))
                            {
                                return (false, $"Gagal menyimpan file {documentName}", uploadedCount);
                            }

                            var fileInfo = new FileInfo(filePath);
                            if (fileInfo.Length != file.Length)
                            {
                                System.IO.File.Delete(filePath); // Clean up incomplete file
                                return (false, $"File {documentName} tidak tersimpan dengan lengkap", uploadedCount);
                            }

                            // Create document record
                            var document = new PermitDocument
                            {
                                PermitApplicationId = permitId,
                                DocumentName = documentName,
                                DocumentType = documentType,
                                FilePath = $"/documents/supporting/{uniqueFileName}",
                                FileSize = file.Length,
                                FileExtension = fileExtension,
                                UploadDate = DateTime.Now,
                                UploadedByUserId = userId
                            };

                            uploadedDocuments.Add(document);
                            uploadedCount++;
                        }
                        catch (Exception ex)
                        {
                            // Clean up any partially uploaded files
                            foreach (var doc in uploadedDocuments)
                            {
                                var existingFile = Path.Combine(_environment.WebRootPath, doc.FilePath.TrimStart('/'));
                                if (System.IO.File.Exists(existingFile))
                                {
                                    try { System.IO.File.Delete(existingFile); } catch { }
                                }
                            }

                            return (false, $"Gagal mengupload {documentName}: {ex.Message}", 0);
                        }
                    }
                }

                // Save all documents to database
                if (uploadedDocuments.Any())
                {
                    _context.PermitDocuments.AddRange(uploadedDocuments);
                    await _context.SaveChangesAsync();
                }

                return (true, "", uploadedCount);
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan sistem: {ex.Message}", 0);
            }
        }

        // Helper method untuk sanitize filename
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "unknown";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            // Remove any remaining problematic characters
            sanitized = sanitized.Replace(" ", "_")
                                .Replace("..", "_")
                                .Replace("__", "_")
                                .Trim('_');

            return string.IsNullOrWhiteSpace(sanitized) ? "document" : sanitized;
        }
        // Document validation helper
        // REPLACE METHOD ValidateRequiredDocuments DI PermitController.cs

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
                    errors.Add($"❌ {documentName} wajib diupload");
                    missingCount++;
                }
                else
                {
                    var fileValidation = ValidateUploadedFile(file);
                    if (!fileValidation.IsValid)
                    {
                        errors.Add($"❌ {documentName}: {string.Join(", ", fileValidation.Errors)}");
                    }
                    else
                    {
                        // Optional: Log successful validation for debugging
                        Console.WriteLine($"✅ {documentName} valid ({FormatFileSize(file.Length)})");
                    }
                }
            }

            if (missingCount > 0)
            {
                errors.Insert(0, $"📋 Total dokumen yang belum diupload: {missingCount} dari {requiredDocuments.Length}");
            }

            return (errors.Count == 0, errors);
        }

        // Helper method untuk format file size
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

        // REPLACE METHOD ValidateUploadedFile DI PermitController.cs

        private (bool IsValid, List<string> Errors) ValidateUploadedFile(IFormFile file)
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
        "image/pjpeg" // IE compatibility
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

        // Helper method untuk validasi file gambar
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

            // Check access rights
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
                OriginLocation = permit.OriginLocation,
                DestinationLocation = permit.DestinationLocation,
                DeparturePort = permit.DeparturePort,
                ArrivalPort = permit.ArrivalPort,
                RejectionReason = permit.RejectionReason,
                ValidFrom = permit.ValidFrom,
                ValidUntil = permit.ValidUntil,
                GeneratedDocumentPath = permit.GeneratedDocumentPath,
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
                Documents = permit.Documents.Select(d => new DocumentViewModel
                {
                    Id = d.Id,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    FilePath = d.FilePath,
                    FileSize = d.FileSize,
                    FileExtension = d.FileExtension,
                    UploadDate = d.UploadDate,
                    UploadedBy = d.UploadedByUser.NamaLengkap
                }).OrderBy(d => d.DocumentType).ToList(),
                CanDownload = permit.Status == PermitStatus.FinalApproved && !string.IsNullOrEmpty(permit.GeneratedDocumentPath),
                CanApprove = CanUserApprove(userRole, permit.Status)
            };

            return View(model);
        }

        // TAMBAHKAN METHOD DEBUG INI DI PermitController.cs

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

        // Method untuk clean up orphaned files
        [HttpPost]
        public async Task<IActionResult> CleanupOrphanedFiles()
        {
            if (_environment.IsDevelopment())
            {
                var userId = GetCurrentUserId();
                if (userId == null) return RedirectToAction("Login", "Auth");

                try
                {
                    var supportingPath = Path.Combine(_environment.WebRootPath, "documents", "supporting");
                    var results = new List<string>();

                    if (Directory.Exists(supportingPath))
                    {
                        var files = Directory.GetFiles(supportingPath);
                        var dbDocuments = await _context.PermitDocuments.Select(d => d.FilePath).ToListAsync();

                        foreach (var file in files)
                        {
                            var relativePath = "/" + Path.GetRelativePath(_environment.WebRootPath, file).Replace("\\", "/");

                            if (!dbDocuments.Contains(relativePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(file);
                                    results.Add($"🗑️ Deleted orphaned file: {Path.GetFileName(file)}");
                                }
                                catch (Exception ex)
                                {
                                    results.Add($"❌ Failed to delete {Path.GetFileName(file)}: {ex.Message}");
                                }
                            }
                        }
                    }

                    TempData["SuccessMessage"] = $"Cleanup completed. {results.Count} operations performed.";
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

        // GET: Permit/Approve/5 - Form approval dengan preview dokumen
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
                    UploadedBy = d.UploadedByUser.NamaLengkap
                }).OrderBy(d => d.DocumentType).ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> DownloadDocument(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var document = await _context.PermitDocuments
                .Include(d => d.PermitApplication)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                TempData["ErrorMessage"] = "Dokumen tidak ditemukan";
                return NotFound();
            }

            var userRole = HttpContext.Session.GetString("Role");

            // Check permission
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
                var contentType = GetContentType(document.FileExtension);
                var fileName = $"{document.DocumentName}{document.FileExtension}";

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mengunduh dokumen";
                return RedirectToAction("Detail", new { id = document.PermitApplicationId });
            }
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

            // Check permission to view document
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

            // Generate fresh PDF content if needed or document doesn't exist
            if (string.IsNullOrEmpty(permit.GeneratedDocumentPath))
            {
                await GeneratePermitDocument(permit);
                await _context.SaveChangesAsync();
            }

            // Create view model for document viewer
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

        // Method untuk mendapatkan konten dokumen sebagai HTML
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

        // POST: Permit/Approve - Proses approval
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
                var permit = await _context.PermitApplications
                    .Include(p => p.User)
                    .Include(p => p.LivestockDetails)
                    .Include(p => p.Documents)
                    .FirstOrDefaultAsync(p => p.Id == model.Id);

                if (permit == null)
                {
                    TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                    return RedirectToAction("Index");
                }

                if (!CanUserApprove(userRole, permit.Status))
                {
                    TempData["ErrorMessage"] = "Permohonan tidak dapat diproses pada tahap ini";
                    return RedirectToAction("Detail", new { id = model.Id });
                }

                // Validate documents for admin approval
                if (userRole == "Admin" && model.Action == "Approve" && permit.Documents.Count < 7)
                {
                    TempData["ErrorMessage"] = "Dokumen pendukung belum lengkap. Permohonan tidak dapat disetujui.";
                    return RedirectToAction("Approve", new { id = model.Id });
                }

                var fromStatus = permit.Status;
                PermitStatus toStatus;
                string actionText;

                if (model.Action == "Approve")
                {
                    if (userRole == "Admin")
                    {
                        toStatus = PermitStatus.AdminApproved;
                        actionText = "Disetujui Admin";
                        permit.AdminId = userId.Value;
                        permit.AdminApprovalDate = DateTime.Now;
                        permit.CurrentApprovalLevel = 2;

                        // Generate PDF document setelah admin approve
                        await GeneratePermitDocument(permit);
                    }
                    else if (userRole == "Verifikator")
                    {
                        toStatus = PermitStatus.VerifikatorApproved;
                        actionText = "Disetujui Verifikator";
                        permit.VerifikatorId = userId.Value;
                        permit.VerificationDate = DateTime.Now;
                        permit.CurrentApprovalLevel = 3;
                    }
                    else // KepalaDinas
                    {
                        toStatus = PermitStatus.FinalApproved;
                        actionText = "Disetujui Kepala Dinas";
                        permit.KepalaDinasId = userId.Value;
                        permit.FinalApprovalDate = DateTime.Now;
                        permit.ValidFrom = DateTime.Now;
                        permit.ValidUntil = DateTime.Now.AddMonths(6); // Valid 6 bulan
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

                    permit.RejectionReason = model.Comments;
                }

                permit.Status = toStatus;

                // Add approval history
                var history = new PermitApprovalHistory
                {
                    PermitApplicationId = permit.Id,
                    UserId = userId.Value,
                    FromStatus = fromStatus,
                    ToStatus = toStatus,
                    Action = actionText,
                    Comments = model.Comments,
                    ActionDate = DateTime.Now
                };

                _context.PermitApprovalHistories.Add(history);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Permohonan {permit.ApplicationNumber} berhasil {actionText.ToLower()}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Terjadi kesalahan saat memproses approval. Silakan coba lagi.");
                return View(model);
            }
        }

        // GET: Permit/Download/5 - Download PDF (hanya untuk User dengan status FinalApproved)
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

            // Check access rights - hanya User yang bisa download
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

            // User hanya bisa download jika sudah final approved
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

        private async Task<string> GenerateApplicationNumber()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;

            // Gunakan transaction untuk memastikan atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Lock table untuk mencegah race condition
                await _context.Database.ExecuteSqlRawAsync("SELECT 1 FROM PermitApplications WITH (TABLOCKX)");

                // Ambil nomor terakhir dengan pattern yang benar untuk bulan/tahun ini
                var prefix = $"%/03-260/DPM&PTSP/{year}";

                var lastApplication = await _context.PermitApplications
                    .Where(p => p.ApplicationNumber.EndsWith($"/03-260/DPM&PTSP/{year}") &&
                               p.SubmissionDate.Year == year &&
                               p.SubmissionDate.Month == month)
                    .OrderByDescending(p => p.ApplicationNumber)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;

                if (lastApplication != null)
                {
                    // Extract nomor urut dari ApplicationNumber (format: XXX/03-260/DPM&PTSP/YYYY)
                    var numberPart = lastApplication.ApplicationNumber.Split('/')[0];
                    if (int.TryParse(numberPart, out int currentNumber))
                    {
                        nextNumber = currentNumber + 1;
                    }
                }

                // Generate nomor dengan retry mechanism
                string applicationNumber;
                int maxRetries = 10;
                int retryCount = 0;

                do
                {
                    applicationNumber = $"{nextNumber.ToString().PadLeft(3, '0')}/03-260/DPM&PTSP/{year}";

                    // Check apakah sudah ada
                    var exists = await _context.PermitApplications
                        .AnyAsync(p => p.ApplicationNumber == applicationNumber);

                    if (!exists)
                    {
                        break; // Nomor unik ditemukan
                    }

                    nextNumber++;
                    retryCount++;

                    if (retryCount >= maxRetries)
                    {
                        throw new InvalidOperationException("Tidak dapat menggenerate nomor aplikasi unik setelah beberapa percobaan");
                    }

                } while (true);

                await transaction.CommitAsync();
                return applicationNumber;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        private bool CanUserApprove(string userRole, PermitStatus status)
        {
            return userRole switch
            {
                "Admin" => status == PermitStatus.Submitted || status == PermitStatus.UnderAdminReview,
                "Verifikator" => status == PermitStatus.AdminApproved || status == PermitStatus.UnderVerifikatorReview,
                "KepalaDinas" => status == PermitStatus.VerifikatorApproved || status == PermitStatus.PendingKepalaDinas,
                _ => false
            };
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


        // Method untuk mendapatkan content type berdasarkan ekstensi file
        private string GetContentType(string fileExtension)
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

        #endregion
    }
}