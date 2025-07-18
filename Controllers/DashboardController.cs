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
    }
}