using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;

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
            // Cek apakah user sudah login
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Ambil informasi user
            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            // Buat dashboard stats berdasarkan role
            var stats = await GetDashboardStats(user);

            ViewBag.UserRole = user.Role;
            ViewBag.UserName = user.NamaLengkap;

            return View(stats);
        }

        private async Task<DashboardStatsViewModel> GetDashboardStats(User user)
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var stats = new DashboardStatsViewModel();

            if (user.Role == "User")
            {
                // Stats untuk User
                stats.TotalApplications = await _context.PermitApplications
                    .CountAsync(p => p.UserId == user.Id);

                stats.PendingVerification = await _context.PermitApplications
                    .CountAsync(p => p.UserId == user.Id &&
                                (p.Status == PermitStatus.Submitted || p.Status == PermitStatus.UnderReview));

                stats.PendingKepalaDinas = await _context.PermitApplications
                    .CountAsync(p => p.UserId == user.Id &&
                                (p.Status == PermitStatus.VerifikatorApproved || p.Status == PermitStatus.PendingKepalaDinas));

                stats.ApprovedThisMonth = await _context.PermitApplications
                    .CountAsync(p => p.UserId == user.Id &&
                                p.Status == PermitStatus.FinalApproved &&
                                p.FinalApprovalDate.HasValue &&
                                p.FinalApprovalDate.Value.Month == currentMonth &&
                                p.FinalApprovalDate.Value.Year == currentYear);

                stats.RejectedThisMonth = await _context.PermitApplications
                    .CountAsync(p => p.UserId == user.Id &&
                                (p.Status == PermitStatus.VerifikatorRejected || p.Status == PermitStatus.KepalaDinasRejected) &&
                                p.SubmissionDate.Month == currentMonth &&
                                p.SubmissionDate.Year == currentYear);

                // Recent applications untuk user
                stats.RecentApplications = await _context.PermitApplications
                    .Where(p => p.UserId == user.Id)
                    .OrderByDescending(p => p.SubmissionDate)
                    .Take(5)
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
                        CanDownload = p.Status == PermitStatus.FinalApproved && !string.IsNullOrEmpty(p.GeneratedDocumentPath),
                        CanView = true,
                        CanApprove = false
                    })
                    .ToListAsync();
            }
            else if (user.Role == "Verifikator")
            {
                // Stats untuk Verifikator
                stats.TotalApplications = await _context.PermitApplications.CountAsync();

                stats.PendingVerification = await _context.PermitApplications
                    .CountAsync(p => p.Status == PermitStatus.Submitted || p.Status == PermitStatus.UnderReview);

                stats.PendingKepalaDinas = await _context.PermitApplications
                    .CountAsync(p => p.Status == PermitStatus.VerifikatorApproved || p.Status == PermitStatus.PendingKepalaDinas);

                stats.ApprovedThisMonth = await _context.PermitApplications
                    .CountAsync(p => p.Status == PermitStatus.VerifikatorApproved &&
                                p.VerificationDate.HasValue &&
                                p.VerificationDate.Value.Month == currentMonth &&
                                p.VerificationDate.Value.Year == currentYear);

                stats.RejectedThisMonth = await _context.PermitApplications
                    .CountAsync(p => p.Status == PermitStatus.VerifikatorRejected &&
                                p.VerificationDate.HasValue &&
                                p.VerificationDate.Value.Month == currentMonth &&
                                p.VerificationDate.Value.Year == currentYear);

                // Recent applications yang perlu diverifikasi
                stats.RecentApplications = await _context.PermitApplications
                    .Where(p => p.Status == PermitStatus.Submitted || p.Status == PermitStatus.UnderReview)
                    .OrderByDescending(p => p.SubmissionDate)
                    .Take(5)
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
                        CanApprove = true
                    })
                    .ToListAsync();

                // Pending approvals untuk verifikator
                stats.MyPendingApprovals = stats.RecentApplications;
            }
            else if (user.Role == "KepalaDinas")
            {
                // Stats untuk Kepala Dinas
                stats.TotalApplications = await _context.PermitApplications.CountAsync();

                stats.PendingVerification = await _context.PermitApplications
                    .CountAsync(p => p.Status == PermitStatus.Submitted || p.Status == PermitStatus.UnderReview);

                stats.PendingKepalaDinas = await _context.PermitApplications
                    .CountAsync(p => p.Status == PermitStatus.VerifikatorApproved || p.Status == PermitStatus.PendingKepalaDinas);

                stats.ApprovedThisMonth = await _context.PermitApplications
                    .CountAsync(p => p.Status == PermitStatus.FinalApproved &&
                                p.FinalApprovalDate.HasValue &&
                                p.FinalApprovalDate.Value.Month == currentMonth &&
                                p.FinalApprovalDate.Value.Year == currentYear);

                stats.RejectedThisMonth = await _context.PermitApplications
                    .CountAsync(p => p.Status == PermitStatus.KepalaDinasRejected &&
                                p.FinalApprovalDate.HasValue &&
                                p.FinalApprovalDate.Value.Month == currentMonth &&
                                p.FinalApprovalDate.Value.Year == currentYear);

                // Recent applications (semua)
                stats.RecentApplications = await _context.PermitApplications
                    .OrderByDescending(p => p.SubmissionDate)
                    .Take(5)
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
                        CanDownload = p.Status == PermitStatus.FinalApproved && !string.IsNullOrEmpty(p.GeneratedDocumentPath),
                        CanView = true,
                        CanApprove = p.Status == PermitStatus.VerifikatorApproved || p.Status == PermitStatus.PendingKepalaDinas
                    })
                    .ToListAsync();

                // Pending approvals untuk kepala dinas
                stats.MyPendingApprovals = await _context.PermitApplications
                    .Where(p => p.Status == PermitStatus.VerifikatorApproved || p.Status == PermitStatus.PendingKepalaDinas)
                    .OrderByDescending(p => p.VerificationDate)
                    .Take(5)
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
                        CanApprove = true
                    })
                    .ToListAsync();
            }

            return stats;
        }

        // Method untuk mengecek otentikasi (bisa dipanggil dari action lain)
        private bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }
    }
}