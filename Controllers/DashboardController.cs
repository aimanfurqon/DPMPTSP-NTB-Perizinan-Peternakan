using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models; // Pastikan namespace ini benar
using PerizinanPeternakan.ViewModels;
using System.Linq;

namespace PerizinanPeternakan.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction(nameof(AuthController.Login), "Auth");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction(nameof(AuthController.Login), "Auth");
            }

            ViewData["UserName"] = user.NamaLengkap;
            ViewData["UserRole"] = user.Role;

            var stats = await GetDashboardStatsAsync(user);

            return View(stats);
        }

        public async Task<IActionResult> KepalaDinasDashboard()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction(nameof(AuthController.Login), "Auth");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction(nameof(AuthController.Login), "Auth");
            }

            if (user.Role != "KepalaDinas")
            {
                TempData["ErrorMessage"] = "Akses ditolak. Hanya Kepala Dinas yang dapat mengakses halaman ini.";
                return RedirectToAction("Index");
            }

            ViewData["UserName"] = user.NamaLengkap;
            ViewData["UserRole"] = user.Role;

            var dashboardData = await GetKepalaDinasDashboardDataAsync();
            return View(dashboardData);
        }

        private async Task<DashboardStatsViewModel> GetDashboardStatsAsync(User user)
        {
            var stats = new DashboardStatsViewModel();

            // PERBAIKAN: Menggunakan LivestockPermitApplication sesuai dengan DbContext Anda
            IQueryable<LivestockPermitApplication> baseQuery = _context.PermitApplications;

            if (user.Role == "User")
            {
                baseQuery = baseQuery.Where(p => p.UserId == user.Id);
            }

            var applicationsData = await baseQuery
                .Select(p => new
                {
                    p.Id,
                    p.ApplicationNumber,
                    p.CompanyName,
                    ApplicantName = p.User.NamaLengkap,
                    p.Status,
                    p.SubmissionDate,
                    p.OriginLocation,
                    p.DestinationLocation,
                    p.AdminApprovalDate,
                    p.VerificationDate,
                    p.FinalApprovalDate
                })
                .ToListAsync();

            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            stats.TotalApplications = applicationsData.Count;
            stats.PendingAdminReview = applicationsData.Count(p => p.Status == PermitStatus.Submitted || p.Status == PermitStatus.UnderAdminReview);
            stats.PendingVerifikatorReview = applicationsData.Count(p => p.Status == PermitStatus.AdminApproved || p.Status == PermitStatus.UnderVerifikatorReview);
            stats.PendingKepalaDinas = applicationsData.Count(p => p.Status == PermitStatus.VerifikatorApproved || p.Status == PermitStatus.PendingKepalaDinas);
            stats.ApprovedThisMonth = applicationsData.Count(p => p.Status == PermitStatus.FinalApproved && p.FinalApprovalDate.HasValue && p.FinalApprovalDate.Value.Month == currentMonth && p.FinalApprovalDate.Value.Year == currentYear);

            stats.RecentApplications = applicationsData
                .OrderByDescending(p => p.SubmissionDate)
                .Take(5)
                .Select(p => new PermitListViewModel
                {
                    Id = p.Id,
                    ApplicationNumber = p.ApplicationNumber,
                    CompanyName = p.CompanyName,
                    ApplicantName = p.ApplicantName,
                    Status = p.Status,
                    SubmissionDate = p.SubmissionDate,
                    OriginLocation = p.OriginLocation,
                    DestinationLocation = p.DestinationLocation
                }).ToList();

            if (user.Role != "User")
            {
                stats.MyPendingApprovals = applicationsData
                    .Where(p =>
                        (user.Role == "Admin" && (p.Status == PermitStatus.Submitted || p.Status == PermitStatus.UnderAdminReview)) ||
                        (user.Role == "Verifikator" && (p.Status == PermitStatus.AdminApproved || p.Status == PermitStatus.UnderVerifikatorReview)) ||
                        (user.Role == "KepalaDinas" && (p.Status == PermitStatus.VerifikatorApproved || p.Status == PermitStatus.PendingKepalaDinas))
                    )
                    .OrderByDescending(p => p.SubmissionDate)
                    .Take(5)
                    .Select(p => new PermitListViewModel
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.ApplicantName,
                        Status = p.Status,
                        SubmissionDate = p.SubmissionDate,
                        OriginLocation = p.OriginLocation,
                        DestinationLocation = p.DestinationLocation
                    }).ToList();
            }

            return stats;
        }

        private async Task<KepalaDinasDashboardViewModel> GetKepalaDinasDashboardDataAsync()
        {
            var dashboardData = new KepalaDinasDashboardViewModel();
            var currentYear = DateTime.Now.Year;
            var currentMonth = DateTime.Now.Month;

            // Get quota data for the current year
            var quotaData = await _context.LivestockQuotas
                .Where(q => q.Year == currentYear)
                .ToListAsync();

            // Get permit applications for the current year
            var permitData = await _context.PermitApplications
                .Where(p => p.SubmissionDate.Year == currentYear && p.Status == PermitStatus.FinalApproved)
                .Include(p => p.LivestockDetails)
                .ToListAsync();

            // Prepare monthly quota vs realization data
            var monthlyQuotaData = new List<MonthlyQuotaData>();
            for (int month = 1; month <= 12; month++)
            {
                // For now, we'll use a simplified approach since quota is not monthly
                // We'll distribute the total quota evenly across months or use a fixed monthly quota
                var totalQuota = quotaData.Sum(q => q.TotalQuota);
                var monthlyQuota = totalQuota / 12; // Distribute evenly
                
                var monthRealization = permitData
                    .Where(p => p.FinalApprovalDate?.Month == month)
                    .Sum(p => p.LivestockDetails.Sum(ld => ld.Quantity));

                monthlyQuotaData.Add(new MonthlyQuotaData
                {
                    Month = month,
                    MonthName = GetMonthName(month),
                    Quota = monthlyQuota,
                    Realization = monthRealization,
                    Percentage = monthlyQuota > 0 ? (monthRealization * 100.0 / monthlyQuota) : 0
                });
            }

            dashboardData.MonthlyQuotaData = monthlyQuotaData;

            // Prepare monthly permit classification data
            var monthlyPermitData = new List<MonthlyPermitData>();
            for (int month = 1; month <= 12; month++)
            {
                var monthPermits = permitData.Where(p => p.FinalApprovalDate?.Month == month).ToList();
                
                var classificationData = monthPermits
                    .GroupBy(p => p.OriginLocation.Split(',')[0].Trim()) // Get province from origin
                    .Select(g => new PermitClassificationData
                    {
                        Origin = g.Key,
                        Count = g.Count(),
                        TotalQuantity = g.Sum(p => p.LivestockDetails.Sum(ld => ld.Quantity))
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                monthlyPermitData.Add(new MonthlyPermitData
                {
                    Month = month,
                    MonthName = GetMonthName(month),
                    TotalPermits = monthPermits.Count,
                    TotalQuantity = monthPermits.Sum(p => p.LivestockDetails.Sum(ld => ld.Quantity)),
                    Classifications = classificationData
                });
            }

            dashboardData.MonthlyPermitData = monthlyPermitData;

            // Calculate summary statistics
            dashboardData.TotalQuota = quotaData.Sum(q => q.TotalQuota);
            dashboardData.TotalRealization = permitData.Sum(p => p.LivestockDetails.Sum(ld => ld.Quantity));
            dashboardData.TotalPermits = permitData.Count;
            dashboardData.AverageProcessingDays = await CalculateAverageProcessingDaysAsync();

            // Get top origins
            dashboardData.TopOrigins = permitData
                .GroupBy(p => p.OriginLocation.Split(',')[0].Trim())
                .Select(g => new TopOriginData
                {
                    Origin = g.Key,
                    Count = g.Count(),
                    TotalQuantity = g.Sum(p => p.LivestockDetails.Sum(ld => ld.Quantity))
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            return dashboardData;
        }

        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "Januari",
                2 => "Februari",
                3 => "Maret",
                4 => "April",
                5 => "Mei",
                6 => "Juni",
                7 => "Juli",
                8 => "Agustus",
                9 => "September",
                10 => "Oktober",
                11 => "November",
                12 => "Desember",
                _ => "Unknown"
            };
        }

        private async Task<double> CalculateAverageProcessingDaysAsync()
        {
            var completedPermits = await _context.PermitApplications
                .Where(p => p.Status == PermitStatus.FinalApproved && 
                           p.FinalApprovalDate.HasValue)
                .Select(p => new
                {
                    p.SubmissionDate,
                    p.FinalApprovalDate
                })
                .ToListAsync();

            if (!completedPermits.Any())
                return 0;

            var totalDays = completedPermits.Sum(p => 
                (p.FinalApprovalDate.Value - p.SubmissionDate).TotalDays);

            return totalDays / completedPermits.Count;
        }
    }
}