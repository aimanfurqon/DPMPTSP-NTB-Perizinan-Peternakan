using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
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
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");
            if (HttpContext.Session.GetString("Role") != "User")
            {
                TempData["ErrorMessage"] = "Hanya user yang dapat membuat permohonan";
                return RedirectToAction("Index");
            }

            try
            {
                // ===============================================
                // STEP 1: NORMALIZE APPLICANT TYPE
                // ===============================================

                if (string.IsNullOrEmpty(model.ApplicantType))
                {
                    model.ApplicantType = "Company";
                }

                Console.WriteLine($"🔍 ApplicantType received: '{model.ApplicantType}'");

                // ===============================================
                // STEP 2: REMOVE PROBLEMATIC MODELSTATE KEYS FIRST
                // ===============================================

                // Remove keys that will cause validation issues before mapping
                var keysToRemove = new[] {
            "OriginLocation",
            "DestinationLocation",
            "DeparturePort",
            "ArrivalPort",
            "CompanyName",        // Remove this temporarily for Individual
            "CompanyAddress",     // Remove this temporarily for Individual
            "CompanyProvince",    // Remove this temporarily for Individual
            "CompanyRegency"      // Remove this temporarily for Individual
        };

                foreach (var key in keysToRemove)
                {
                    ModelState.Remove(key);
                }

                // ===============================================
                // STEP 3: VALIDATE AND MAP BASED ON APPLICANT TYPE
                // ===============================================

                if (model.ApplicantType == "Individual")
                {
                    Console.WriteLine("👤 Processing Individual applicant...");

                    // Validate individual fields FIRST
                    var individualErrors = new List<string>();

                    if (string.IsNullOrWhiteSpace(model.IndividualName))
                    {
                        individualErrors.Add("Nama lengkap wajib diisi");
                        ModelState.AddModelError("IndividualName", "Nama lengkap wajib diisi");
                    }

                    if (string.IsNullOrWhiteSpace(model.IndividualProvince))
                    {
                        individualErrors.Add("Provinsi wajib diisi");
                        ModelState.AddModelError("IndividualProvince", "Provinsi wajib diisi");
                    }

                    if (string.IsNullOrWhiteSpace(model.IndividualRegency))
                    {
                        individualErrors.Add("Kabupaten wajib diisi");
                        ModelState.AddModelError("IndividualRegency", "Kabupaten wajib diisi");
                    }

                    if (string.IsNullOrWhiteSpace(model.IndividualAddress))
                    {
                        individualErrors.Add("Alamat lengkap wajib diisi");
                        ModelState.AddModelError("IndividualAddress", "Alamat lengkap wajib diisi");
                    }

                    // If individual validation passes, map to company fields
                    if (individualErrors.Count == 0)
                    {
                        model.CompanyName = model.IndividualName.Trim();
                        model.CompanyAddress = $"{model.IndividualAddress.Trim()}, {model.IndividualRegency.Trim()}, {model.IndividualProvince.Trim()}";
                        model.CompanyProvince = model.IndividualProvince.Trim();
                        model.CompanyRegency = model.IndividualRegency.Trim();

                        Console.WriteLine($"✅ Individual data mapped - Name: '{model.CompanyName}', Address: '{model.CompanyAddress}'");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Individual validation failed: {string.Join(", ", individualErrors)}");
                    }
                }
                else if (model.ApplicantType == "Company")
                {
                    Console.WriteLine("🏢 Processing Company applicant...");

                    // Validate company fields
                    if (string.IsNullOrWhiteSpace(model.CompanyName))
                    {
                        ModelState.AddModelError("CompanyName", "Nama perusahaan wajib diisi");
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
                // STEP 4: FINAL VALIDATION AFTER MAPPING
                // ===============================================

                // Now validate the mapped/processed data
                if (string.IsNullOrWhiteSpace(model.CompanyName))
                {
                    var fieldName = model.ApplicantType == "Individual" ? "nama lengkap" : "nama perusahaan";
                    ModelState.AddModelError("CompanyName", $"Field {fieldName} harus diisi");
                    Console.WriteLine($"❌ Final validation failed: CompanyName is empty after mapping");
                }

                if (string.IsNullOrWhiteSpace(model.CompanyAddress))
                {
                    var fieldName = model.ApplicantType == "Individual" ? "alamat lengkap" : "alamat perusahaan";
                    ModelState.AddModelError("CompanyAddress", $"Field {fieldName} harus diisi");
                    Console.WriteLine($"❌ Final validation failed: CompanyAddress is empty after mapping");
                }

                // ===============================================
                // STEP 5: VALIDATE LIVESTOCK AND DOCUMENTS
                // ===============================================

                // Validate livestock details
                if (model.LivestockDetails == null || !model.LivestockDetails.Any(d => !string.IsNullOrEmpty(d.LivestockType) && d.Quantity > 0))
                {
                    ModelState.AddModelError("", "Minimal harus ada satu detail ternak yang valid");
                }

                // Validate required documents
                if (!ValidateAllDocumentsUploaded(model))
                {
                    ModelState.AddModelError("", "Semua dokumen wajib harus diupload");
                }

                // Validate document details
                if (!ValidateDocumentDetails(model))
                {
                    ModelState.AddModelError("", "Detail dokumen (tanggal dan nomor) harus diisi dengan benar");
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
                // STEP 7: PROCESS SUCCESSFUL SUBMISSION
                // ===============================================

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    ModelState.AddModelError("", "User tidak ditemukan");
                    return View(model);
                }

                var applicationNumber = await GenerateApplicationNumber();

                Console.WriteLine($"✅ Creating permit application - Number: {applicationNumber}");

                var permitApplication = new LivestockPermitApplication
                {
                    ApplicationNumber = applicationNumber,
                    UserId = userId.Value,

                    CompanyName = model.CompanyName.Trim(),
                    CompanyAddress = model.CompanyAddress,

                    OriginLocation = model.OriginLocation?.Trim() ?? "",
                    DestinationLocation = model.DestinationLocation?.Trim() ?? "",
                    DeparturePort = model.DeparturePort?.Trim() ?? "",
                    ArrivalPort = model.ArrivalPort?.Trim() ?? "",

                    Status = PermitStatus.Submitted,
                    SubmissionDate = DateTime.Now,
                    CurrentApprovalLevel = 1,

                    OriginProvinceId = model.OriginProvinceId,
                    OriginRegencyId = model.OriginRegencyId,
                    DestinationProvinceId = model.DestinationProvinceId,
                    DestinationRegencyId = model.DestinationRegencyId
                };

                // Add livestock details
                foreach (var livestockDetail in model.LivestockDetails.Where(d => !string.IsNullOrEmpty(d.LivestockType) && d.Quantity > 0))
                {
                    permitApplication.LivestockDetails.Add(new LivestockDetail
                    {
                        LivestockType = livestockDetail.LivestockType.Trim(),
                        Quantity = livestockDetail.Quantity,
                        Description = livestockDetail.Description?.Trim()
                    });
                }

                // Save permit application
                _context.PermitApplications.Add(permitApplication);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Permit application saved with ID: {permitApplication.Id}");

                // Upload supporting documents
                var uploadResult = await UploadSupportingDocuments(permitApplication.Id, model, userId.Value);

                if (!uploadResult.Success)
                {
                    _context.PermitApplications.Remove(permitApplication);
                    await _context.SaveChangesAsync();

                    ModelState.AddModelError("", uploadResult.ErrorMessage);

                    if (model.LivestockDetails == null || !model.LivestockDetails.Any())
                    {
                        model.LivestockDetails.Add(new LivestockDetailViewModel());
                    }

                    return View(model);
                }

                // Add approval history
                var approvalHistory = new PermitApprovalHistory
                {
                    PermitApplicationId = permitApplication.Id,
                    UserId = userId.Value,
                    FromStatus = PermitStatus.Draft,
                    ToStatus = PermitStatus.Submitted,
                    Action = "Submit",
                    Comments = model.ApplicantType == "Individual"
                        ? $"Permohonan izin diajukan oleh pemohon perorangan: {model.IndividualName}"
                        : $"Permohonan izin diajukan oleh perusahaan: {model.CompanyName}",
                    ActionDate = DateTime.Now
                };

                _context.PermitApprovalHistories.Add(approvalHistory);
                await _context.SaveChangesAsync();

                // Success message
                var applicantTypeText = model.ApplicantType == "Individual" ? "perorangan" : "perusahaan";
                var applicantName = model.ApplicantType == "Individual" ? model.IndividualName : model.CompanyName;

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

                if (model.LivestockDetails == null || !model.LivestockDetails.Any())
                {
                    model.LivestockDetails.Add(new LivestockDetailViewModel());
                }

                return View(model);
            }
        }

        private async Task<(bool Success, string ErrorMessage, int UploadedCount)> UploadSupportingDocuments(int permitId, PermitApplicationViewModel model, int userId)
        {
            var uploadedFiles = new List<string>(); 

            try
            {
                Console.WriteLine($"🚀 Starting document upload process for permit ID: {permitId}");

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
             Date: (DateTime?)null, Number: (string?)null),
                (File: model.DokumenOpsional, Type: "DOKUMEN_OPSIONAL", Name: model.DokumenOpsionalNama ?? "Dokumen Opsional",
     Date: model.DokumenOpsionalTanggal, Number: model.DokumenOpsionalNomor)

        };

                // Create upload directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath, "documents", "supporting");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                int uploadedCount = 0;
                var uploadedDocuments = new List<PermitDocument>();

                // Process each document
                foreach (var (file, type, name, date, number) in documentsToUpload)
                {
                    if (file != null && file.Length > 0)
                    {

                        // Validate file
                        if (!IsValidFile(file))
                        {
                            CleanupUploadedFiles(uploadedFiles);
                            return (false, $"File {name} tidak valid atau tidak memenuhi persyaratan", uploadedCount);
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
                        }
                        catch (Exception fileEx)
                        {
                            CleanupUploadedFiles(uploadedFiles);
                            return (false, $"Gagal menyimpan file {name}: {fileEx.Message}", uploadedCount);
                        }

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
                    else
                    {
                    }
                }

                if (!uploadedDocuments.Any())
                {
                    return (false, "Tidak ada dokumen yang berhasil diupload", 0);
                }

                try
                {
                    _context.PermitDocuments.AddRange(uploadedDocuments);
                    await _context.SaveChangesAsync();

                    return (true, string.Empty, uploadedCount);
                }
                catch (Exception dbEx)
                {
                    CleanupUploadedFiles(uploadedFiles);
                    return (false, $"Gagal menyimpan informasi dokumen ke database: {dbEx.Message}", 0);
                }
            }
            catch (Exception ex)
            {
                CleanupUploadedFiles(uploadedFiles);
                return (false, $"Terjadi kesalahan saat upload dokumen: {ex.Message}", 0);
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

        [HttpPost]
        [Route("Permit/UpdateDocumentDetails")]
        public async Task<IActionResult> UpdateDocumentDetails([FromBody] EditDocumentDetailsRequest request)
        {
            try
            {
                // Validate request
                if (request.DocumentId <= 0)
                {
                    return Json(new { success = false, message = "Document ID tidak valid" });
                }

                // Check authorization
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

                // Get document from database
                var document = await _context.PermitDocuments
                    .FirstOrDefaultAsync(d => d.Id == request.DocumentId);

                if (document == null)
                {
                    return Json(new { success = false, message = "Dokumen tidak ditemukan" });
                }

                // Validate date if provided
                if (request.DocumentDate.HasValue && request.DocumentDate.Value > DateTime.Today)
                {
                    return Json(new { success = false, message = "Tanggal dokumen tidak boleh di masa depan" });
                }

                // Validate document number format if provided
                if (!string.IsNullOrEmpty(request.DocumentNumber) && !IsValidDocumentNumber(request.DocumentNumber))
                {
                    return Json(new { success = false, message = "Format nomor dokumen tidak valid" });
                }

                // Update document details
                document.DocumentDate = request.DocumentDate;
                document.DocumentNumber = request.DocumentNumber?.Trim();
                document.DocumentDescription = request.DocumentDescription?.Trim();

                // Save changes
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Detail dokumen berhasil disimpan",
                    data = new
                    {
                        documentId = document.Id,
                        documentDate = document.DocumentDate?.ToString("yyyy-MM-dd"),
                        documentNumber = document.DocumentNumber,
                        documentDescription = document.DocumentDescription
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating document details: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Terjadi kesalahan server: " + ex.Message
                });
            }
        }


        // Request model untuk edit document details
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

        /// <summary>
        /// Bulk approve permits (for admin operations)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> BulkApprove([FromBody] BulkActionRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole == "User") return Forbid();

            try
            {
                var permits = await _context.PermitApplications
                    .Where(p => request.PermitIds.Contains(p.Id))
                    .ToListAsync();

                var successCount = 0;
                var errors = new List<string>();

                foreach (var permit in permits)
                {
                    if (CanUserApprove(userRole, permit.Status))
                    {
                        var result = await ProcessApproval(permit, "Approve", request.Comments, userId.Value, userRole);
                        if (result.Success)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"{permit.ApplicationNumber}: {result.ErrorMessage}");
                        }
                    }
                    else
                    {
                        errors.Add($"{permit.ApplicationNumber}: Tidak dapat diproses pada tahap ini");
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{successCount} permohonan berhasil disetujui",
                    successCount = successCount,
                    errors = errors
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
        /// Bulk reject permits (for admin operations)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> BulkReject([FromBody] BulkActionRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole == "User") return Forbid();

            try
            {
                var permits = await _context.PermitApplications
                    .Where(p => request.PermitIds.Contains(p.Id))
                    .ToListAsync();

                var successCount = 0;
                var errors = new List<string>();

                foreach (var permit in permits)
                {
                    if (CanUserApprove(userRole, permit.Status))
                    {
                        var result = await ProcessApproval(permit, "Reject", request.Comments, userId.Value, userRole);
                        if (result.Success)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"{permit.ApplicationNumber}: {result.ErrorMessage}");
                        }
                    }
                    else
                    {
                        errors.Add($"{permit.ApplicationNumber}: Tidak dapat diproses pada tahap ini");
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{successCount} permohonan berhasil ditolak",
                    successCount = successCount,
                    errors = errors
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
        /// Validates document details for specific document types
        /// </summary>
        /// <param name="model">The permit application view model</param>
        /// <returns>True if validation passes, false otherwise</returns>
        private bool ValidateDocumentDetails(PermitApplicationViewModel model)
        {
            var errors = new List<string>();

            try
            {
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

                // Validate Dokumen Opsional details (jika ada)
                if (model.DokumenOpsional != null && model.DokumenOpsional.Length > 0)
                {
                    // Validasi tanggal (opsional tapi jika diisi harus valid)
                    if (model.DokumenOpsionalTanggal.HasValue && model.DokumenOpsionalTanggal.Value > DateTime.Today)
                    {
                        errors.Add("Tanggal dokumen opsional tidak boleh di masa depan");
                    }

                    // Validasi nomor dokumen (opsional tapi jika diisi harus valid format)
                    if (!string.IsNullOrWhiteSpace(model.DokumenOpsionalNomor) &&
                        !IsValidDocumentNumber(model.DokumenOpsionalNomor))
                    {
                        errors.Add("Format nomor dokumen opsional tidak valid");
                    }

                    // Validasi nama dokumen (wajib jika upload dokumen)
                    if (string.IsNullOrWhiteSpace(model.DokumenOpsionalNama))
                    {
                        errors.Add("Nama dokumen opsional harus diisi jika mengupload dokumen");
                    }
                }

                // Add errors to ModelState
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                    Console.WriteLine($"❌ Validation error: {error}");
                }

                bool isValid = !errors.Any();
                Console.WriteLine($"📋 Document details validation result: {(isValid ? "PASSED" : "FAILED")}");

                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ValidateDocumentDetails: {ex.Message}");
                ModelState.AddModelError("", "Terjadi kesalahan saat validasi dokumen");
                return false;
            }
        }

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

        /// <summary>
        /// Cleans up uploaded files if there's an error during processing
        /// </summary>
        /// <param name="filePaths">List of file paths to clean up</param>
        private void CleanupUploadedFiles(List<string> filePaths)
        {
            try
            {
                foreach (var filePath in filePaths)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        Console.WriteLine($"🗑️ Cleaned up file: {Path.GetFileName(filePath)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates all required documents are uploaded
        /// </summary>
        /// <param name="model">The permit application view model</param>
        /// <returns>True if all required documents are present, false otherwise</returns>
        private bool ValidateAllDocumentsUploaded(PermitApplicationViewModel model)
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

                var missingDocuments = new List<string>();

                foreach (var doc in requiredDocuments)
                {
                    if (doc.Value == null || doc.Value.Length == 0)
                    {
                        missingDocuments.Add(doc.Key);
                    }
                }

                if (missingDocuments.Any())
                {
                    var errorMessage = $"Dokumen berikut belum diupload: {string.Join(", ", missingDocuments)}";
                    ModelState.AddModelError("", errorMessage);
                    Console.WriteLine($"❌ Missing documents: {string.Join(", ", missingDocuments)}");
                    return false;
                }

                Console.WriteLine("✅ All required documents are uploaded");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error validating documents: {ex.Message}");
                ModelState.AddModelError("", "Terjadi kesalahan saat validasi dokumen");
                return false;
            }
        }

        /// <summary>
        /// Enhanced file validation method
        /// </summary>
        /// <param name="file">The uploaded file</param>
        /// <returns>True if file is valid, false otherwise</returns>
        private new bool IsValidFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    Console.WriteLine("❌ File is null or empty");
                    return false;
                }

                // Check file size (max 5MB)
                const long maxFileSize = 5 * 1024 * 1024; // 5MB
                if (file.Length > maxFileSize)
                {
                    Console.WriteLine($"❌ File size too large: {file.Length} bytes (max: {maxFileSize} bytes)");
                    return false;
                }

                // Check file extension
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                {
                    Console.WriteLine($"❌ Invalid file extension: {fileExtension}");
                    return false;
                }

                // Check MIME type for additional security
                var allowedMimeTypes = new[]
                {
            "application/pdf",
            "image/jpeg",
            "image/jpg",
            "image/png"
        };

                if (!allowedMimeTypes.Contains(file.ContentType?.ToLowerInvariant()))
                {
                    Console.WriteLine($"❌ Invalid MIME type: {file.ContentType}");
                    return false;
                }

                // Check if filename contains invalid characters
                var invalidChars = Path.GetInvalidFileNameChars();
                if (file.FileName.Any(c => invalidChars.Contains(c)))
                {
                    Console.WriteLine($"❌ Filename contains invalid characters: {file.FileName}");
                    return false;
                }

                Console.WriteLine($"✅ File validation passed: {file.FileName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error validating file: {ex.Message}");
                return false;
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

        // ADD API endpoint untuk document details validation
        [HttpPost]
        public async Task<IActionResult> ValidateDocumentDetailsAPI([FromBody] List<DocumentDetailsViewModel> documentDetails)
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

        // Baru banget ini

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

        /// <summary>
        /// Get location suggestions based on user input
        /// Helpful untuk autocomplete provinsi/kabupaten
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLocationSuggestions(string query, string type = "province")
        {
            try
            {
                if (string.IsNullOrEmpty(query) || query.Length < 2)
                {
                    return Json(new { success = true, data = new List<object>() });
                }

                // This is a placeholder - replace with your actual location data source
                var suggestions = new List<object>();

                if (type.ToLower() == "province")
                {
                    // Example province suggestions
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
    }


}