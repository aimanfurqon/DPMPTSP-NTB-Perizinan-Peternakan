// Buat file baru AdminHistoryService.cs di folder Services

using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;

namespace PerizinanPeternakan.Services
{
    public class AdminHistoryService
    {
        private readonly ApplicationDbContext _context;

        public AdminHistoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get admin history dengan filtering
        public async Task<List<AdminHistoryViewModel>> GetAdminHistoryAsync(
            int adminId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            PermitStatus? statusFilter = null,
            string searchTerm = null)
        {
            var query = _context.PermitApplications
                .Where(p => p.AdminId == adminId &&
                           (p.Status >= PermitStatus.AdminApproved || p.Status == PermitStatus.AdminRejected))
                .Include(p => p.User)
                .Include(p => p.ApprovalHistory.Where(h => h.UserId == adminId))
                .Include(p => p.Documents)
                .AsQueryable();

            // Filter berdasarkan tanggal
            if (startDate.HasValue)
            {
                query = query.Where(p => p.AdminApprovalDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.AdminApprovalDate <= endDate.Value);
            }

            // Filter berdasarkan status
            if (statusFilter.HasValue)
            {
                query = query.Where(p => p.Status == statusFilter.Value);
            }

            // Search berdasarkan nomor permohonan, nama pemohon, atau perusahaan
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    p.ApplicationNumber.Contains(searchTerm) ||
                    p.User.NamaLengkap.Contains(searchTerm) ||
                    p.CompanyName.Contains(searchTerm));
            }

            return await query
                .OrderByDescending(p => p.AdminApprovalDate ?? p.SubmissionDate)
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
                        .Where(h => h.UserId == adminId && h.Action.Contains("Admin"))
                        .OrderByDescending(h => h.ActionDate)
                        .Select(h => h.Comments)
                        .FirstOrDefault(),
                    AdminAction = p.ApprovalHistory
                        .Where(h => h.UserId == adminId && h.Action.Contains("Admin"))
                        .OrderByDescending(h => h.ActionDate)
                        .Select(h => h.Action)
                        .FirstOrDefault(),
                    DocumentCount = p.Documents.Count,
                    CanView = true
                })
                .ToListAsync();
        }

        // Get statistik admin
        public async Task<AdminStatisticsViewModel> GetAdminStatisticsAsync(int adminId)
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var totalReviewed = await _context.PermitApplications
                .CountAsync(p => p.AdminId == adminId);

            var totalApproved = await _context.PermitApplications
                .CountAsync(p => p.AdminId == adminId &&
                               (p.Status == PermitStatus.AdminApproved ||
                                p.Status == PermitStatus.VerifikatorApproved ||
                                p.Status == PermitStatus.FinalApproved));

            var totalRejected = await _context.PermitApplications
                .CountAsync(p => p.AdminId == adminId && p.Status == PermitStatus.AdminRejected);

            var thisMonthReviewed = await _context.PermitApplications
                .CountAsync(p => p.AdminId == adminId &&
                               p.AdminApprovalDate.HasValue &&
                               p.AdminApprovalDate.Value.Month == currentMonth &&
                               p.AdminApprovalDate.Value.Year == currentYear);

            var thisMonthApproved = await _context.PermitApplications
                .CountAsync(p => p.AdminId == adminId &&
                               p.AdminApprovalDate.HasValue &&
                               p.AdminApprovalDate.Value.Month == currentMonth &&
                               p.AdminApprovalDate.Value.Year == currentYear &&
                               p.Status != PermitStatus.AdminRejected);

            var averageProcessingTime = await _context.PermitApplications
                .Where(p => p.AdminId == adminId &&
                           p.SubmissionDate != null &&
                           p.AdminApprovalDate.HasValue)
                .Select(p => EF.Functions.DateDiffDay(p.SubmissionDate, p.AdminApprovalDate.Value))
                .AverageAsync();

            return new AdminStatisticsViewModel
            {
                TotalReviewed = totalReviewed,
                TotalApproved = totalApproved,
                TotalRejected = totalRejected,
                ThisMonthReviewed = thisMonthReviewed,
                ThisMonthApproved = thisMonthApproved,
                AverageProcessingDays = Math.Round(averageProcessingTime, 1)
            };
        }

        // Tambah history admin baru
        public async Task<bool> AddAdminHistoryAsync(int permitId, int adminId, string action, string comments)
        {
            try
            {
                var permit = await _context.PermitApplications.FindAsync(permitId);
                if (permit == null) return false;

                var admin = await _context.Users.FindAsync(adminId);
                if (admin == null || admin.Role != "Admin") return false;

                var fromStatus = permit.Status;
                var toStatus = action.Contains("Disetujui") ?
                    PermitStatus.AdminApproved : PermitStatus.AdminRejected;

                var history = new PermitApprovalHistory
                {
                    PermitApplicationId = permitId,
                    UserId = adminId,
                    FromStatus = fromStatus,
                    ToStatus = toStatus,
                    Action = action,
                    Comments = comments,
                    ActionDate = DateTime.Now
                };

                _context.PermitApprovalHistories.Add(history);

                // Update permit
                permit.Status = toStatus;
                permit.AdminId = adminId;
                permit.AdminApprovalDate = DateTime.Now;

                if (toStatus == PermitStatus.AdminRejected)
                {
                    permit.RejectionReason = comments;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Get data untuk chart/grafik
        public async Task<AdminChartDataViewModel> GetAdminChartDataAsync(int adminId, int months = 6)
        {
            var startDate = DateTime.Now.AddMonths(-months);

            var monthlyData = await _context.PermitApplications
                .Where(p => p.AdminId == adminId &&
                           p.AdminApprovalDate.HasValue &&
                           p.AdminApprovalDate.Value >= startDate)
                .GroupBy(p => new {
                    Year = p.AdminApprovalDate.Value.Year,
                    Month = p.AdminApprovalDate.Value.Month
                })
                .Select(g => new MonthlyReviewData
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalReviewed = g.Count(),
                    TotalApproved = g.Count(p => p.Status != PermitStatus.AdminRejected),
                    TotalRejected = g.Count(p => p.Status == PermitStatus.AdminRejected)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            return new AdminChartDataViewModel
            {
                MonthlyData = monthlyData
            };
        }
    }

    // ViewModel untuk statistik admin
    public class AdminStatisticsViewModel
    {
        public int TotalReviewed { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public int ThisMonthReviewed { get; set; }
        public int ThisMonthApproved { get; set; }
        public double AverageProcessingDays { get; set; }

        public double ApprovalRate => TotalReviewed > 0 ?
            Math.Round((double)TotalApproved / TotalReviewed * 100, 1) : 0;
    }

    // ViewModel untuk chart data
    public class AdminChartDataViewModel
    {
        public List<MonthlyReviewData> MonthlyData { get; set; } = new();
    }

    public class MonthlyReviewData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalReviewed { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }

        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }
}