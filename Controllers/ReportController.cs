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
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

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

                    thisMonth = permits.Count(p => p.SubmissionDate.Month == DateTime.Now.Month &&
                                                 p.SubmissionDate.Year == DateTime.Now.Year),
                    thisWeek = permits.Count(p => p.SubmissionDate >= DateTime.Now.AddDays(-7)),
                    today = permits.Count(p => p.SubmissionDate.Date == DateTime.Today),

                    avgProcessingDays = CalculateAverageProcessingTime(permits)
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
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
                return RedirectToAction("Index", "Permit");
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
                var query = GetPermitsQuery(userRole, userId.Value);

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
                    .Take(100)
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

                if (!string.IsNullOrEmpty(statusFilter))
                {
                    if (Enum.TryParse<PermitStatus>(statusFilter, out var status))
                    {
                        query = query.Where(p => p.Status == status);
                    }
                }

                if (!string.IsNullOrEmpty(dateFilter))
                {
                    if (DateTime.TryParse(dateFilter, out var filterDate))
                    {
                        query = query.Where(p => p.SubmissionDate.Date == filterDate.Date);
                    }
                }

                var totalRecords = await query.CountAsync();

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

        #region Private Methods

        private int? GetCurrentUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }

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
                _ => baseQuery.Where(p => false)
            };
        }

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

        #region Request Models

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
    }
}
