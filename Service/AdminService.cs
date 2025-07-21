using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;

namespace PerizinanPeternakan.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminService> _logger;

        public AdminService(ApplicationDbContext context, ILogger<AdminService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(List<AdminHistoryViewModel> History, AdminHistoryStatsViewModel Stats, AdminHistoryPaginationViewModel Pagination)>
            GetAdminHistoryAsync(int adminId, DateTime? startDate, DateTime? endDate,
                PermitStatus? statusFilter, string searchTerm, int page, int pageSize = 10)
        {
            try
            {
                var query = _context.PermitApplications
                    .Where(p => p.AdminId == adminId &&
                               (p.Status >= PermitStatus.AdminApproved || p.Status == PermitStatus.AdminRejected))
                    .Include(p => p.User)
                    .Include(p => p.ApprovalHistory.Where(h => h.UserId == adminId))
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

                // Get statistics
                var allAdminHistory = await _context.PermitApplications
                    .Where(p => p.AdminId == adminId)
                    .ToListAsync();

                var admin = await _context.Users.FindAsync(adminId);

                var stats = new AdminHistoryStatsViewModel
                {
                    AdminName = admin?.NamaLengkap ?? "Unknown",
                    TotalReviewed = allAdminHistory.Count,
                    TotalApproved = allAdminHistory.Count(h =>
                        h.Status == PermitStatus.AdminApproved ||
                        h.Status == PermitStatus.VerifikatorApproved ||
                        h.Status == PermitStatus.FinalApproved),
                    TotalRejected = allAdminHistory.Count(h => h.Status == PermitStatus.AdminRejected),
                    FilteredCount = totalItems
                };

                var pagination = new AdminHistoryPaginationViewModel
                {
                    CurrentPage = page,
                    TotalPages = totalPages,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages,
                    StartDate = startDate?.ToString("yyyy-MM-dd"),
                    EndDate = endDate?.ToString("yyyy-MM-dd"),
                    StatusFilter = statusFilter,
                    SearchTerm = searchTerm
                };

                return (adminHistoryList, stats, pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin history for admin {AdminId}", adminId);
                return (new List<AdminHistoryViewModel>(), new AdminHistoryStatsViewModel(), new AdminHistoryPaginationViewModel());
            }
        }

        public async Task<object> GetAdminStatisticsAsync(int adminId)
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                var stats = new
                {
                    totalReviewed = await _context.PermitApplications.CountAsync(p => p.AdminId == adminId),
                    totalApproved = await _context.PermitApplications.CountAsync(p =>
                        p.AdminId == adminId &&
                        (p.Status == PermitStatus.AdminApproved ||
                         p.Status == PermitStatus.VerifikatorApproved ||
                         p.Status == PermitStatus.FinalApproved)),
                    totalRejected = await _context.PermitApplications.CountAsync(p =>
                        p.AdminId == adminId && p.Status == PermitStatus.AdminRejected),
                    thisMonthReviewed = await _context.PermitApplications.CountAsync(p =>
                        p.AdminId == adminId &&
                        p.AdminApprovalDate.HasValue &&
                        p.AdminApprovalDate.Value.Month == currentMonth &&
                        p.AdminApprovalDate.Value.Year == currentYear)
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin statistics for admin {AdminId}", adminId);
                return new { error = "Failed to load statistics" };
            }
        }

        public async Task<object> GetAdminActivitySummaryAsync(int adminId)
        {
            try
            {
                var today = DateTime.Today;
                var thisWeek = today.AddDays(-7);
                var thisMonth = new DateTime(today.Year, today.Month, 1);

                var summary = new
                {
                    todayReviews = await _context.PermitApprovalHistories
                        .CountAsync(h => h.UserId == adminId &&
                                       h.Action.Contains("Admin") &&
                                       h.ActionDate.Date == today),

                    weekReviews = await _context.PermitApprovalHistories
                        .CountAsync(h => h.UserId == adminId &&
                                       h.Action.Contains("Admin") &&
                                       h.ActionDate >= thisWeek),

                    monthReviews = await _context.PermitApprovalHistories
                        .CountAsync(h => h.UserId == adminId &&
                                       h.Action.Contains("Admin") &&
                                       h.ActionDate >= thisMonth),

                    pendingReviews = await _context.PermitApplications
                        .CountAsync(p => p.Status == PermitStatus.Submitted),

                    avgProcessingTime = await CalculateAverageProcessingTimeAsync(adminId)
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin activity summary for admin {AdminId}", adminId);
                return new { error = ex.Message };
            }
        }

        public async Task<List<object>> GetAdminHistoryChartAsync(int adminId, int months = 6)
        {
            try
            {
                var startDate = DateTime.Now.AddMonths(-months);

                var monthlyData = await _context.PermitApprovalHistories
                    .Where(h => h.UserId == adminId &&
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

                return monthlyData.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin history chart for admin {AdminId}", adminId);
                return new List<object>();
            }
        }

        public async Task<(bool Success, string Message)> AddAdminHistoryAsync(
            int permitId, string action, string comments, int adminId)
        {
            try
            {
                var permit = await _context.PermitApplications.FindAsync(permitId);
                if (permit == null)
                {
                    return (false, "Permohonan tidak ditemukan");
                }

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
                    Comments = comments ?? $"Review admin untuk permohonan {permit.ApplicationNumber}",
                    ActionDate = DateTime.Now
                };

                _context.PermitApprovalHistories.Add(history);

                permit.Status = toStatus;
                permit.AdminId = adminId;
                permit.AdminApprovalDate = DateTime.Now;

                if (toStatus == PermitStatus.AdminRejected)
                {
                    permit.RejectionReason = comments;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin history added for permit {PermitId} by admin {AdminId}", permitId, adminId);

                return (true, $"History admin berhasil ditambahkan untuk {permit.ApplicationNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding admin history for permit {PermitId}", permitId);
                return (false, "Terjadi kesalahan saat menambahkan history");
            }
        }

        public async Task<List<object>> GetAvailablePermitsForTestAsync()
        {
            try
            {
                var availablePermits = await _context.PermitApplications
                    .Where(p => p.Status == PermitStatus.Submitted)
                    .Select(p => new
                    {
                        Value = p.Id.ToString(),
                        Text = $"{p.ApplicationNumber} - {p.CompanyName}"
                    })
                    .ToListAsync();

                return availablePermits.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available permits for test");
                return new List<object>();
            }
        }

        public (List<string> Results, bool Success) TestUploadDirectories(IWebHostEnvironment environment)
        {
            var results = new List<string>();
            var success = true;

            try
            {
                var testPaths = new[]
                {
                    Path.Combine(environment.WebRootPath, "documents"),
                    Path.Combine(environment.WebRootPath, "documents", "supporting"),
                    Path.Combine(environment.WebRootPath, "documents", "permits")
                };

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
                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                        results.Add($"✅ Write permission OK: {path}");
                    }
                    catch (Exception ex)
                    {
                        results.Add($"❌ Error with {path}: {ex.Message}");
                        success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add($"❌ General error: {ex.Message}");
                success = false;
            }

            return (results, success);
        }

        public async Task<List<object>> CheckDocumentsAsync(IWebHostEnvironment environment, int? permitId = null)
        {
            try
            {
                var documentsQuery = _context.PermitDocuments
                    .Include(d => d.PermitApplication)
                    .Include(d => d.UploadedByUser)
                    .AsQueryable();

                if (permitId.HasValue)
                {
                    documentsQuery = documentsQuery.Where(d => d.PermitApplicationId == permitId.Value);
                }

                var documents = await documentsQuery.ToListAsync();

                var results = new List<object>();

                foreach (var doc in documents)
                {
                    var fullPath = Path.Combine(environment.WebRootPath, doc.FilePath.TrimStart('/'));
                    var fileExists = File.Exists(fullPath);

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
                        UploadedBy = doc.UploadedByUser.NamaLengkap,
                        doc.DocumentDate,
                        doc.DocumentNumber,
                        doc.DocumentDescription
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking documents");
                return new List<object>();
            }
        }

        public async Task<(bool Success, List<string> Results)> CleanupOrphanedFilesAsync(IWebHostEnvironment environment)
        {
            try
            {
                var supportingPath = Path.Combine(environment.WebRootPath, "documents", "supporting");
                var results = new List<string>();

                if (Directory.Exists(supportingPath))
                {
                    var files = Directory.GetFiles(supportingPath);
                    var dbDocuments = await _context.PermitDocuments.Select(d => d.FilePath).ToListAsync();

                    foreach (var file in files)
                    {
                        var relativePath = "/" + Path.GetRelativePath(environment.WebRootPath, file).Replace("\\", "/");

                        if (!dbDocuments.Contains(relativePath))
                        {
                            try
                            {
                                File.Delete(file);
                                results.Add($"🗑️ Deleted orphaned file: {Path.GetFileName(file)}");
                            }
                            catch (Exception ex)
                            {
                                results.Add($"❌ Failed to delete {Path.GetFileName(file)}: {ex.Message}");
                            }
                        }
                    }
                }

                _logger.LogInformation("Cleanup completed with {Count} operations", results.Count);
                return (true, results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
                return (false, new List<string> { $"Cleanup failed: {ex.Message}" });
            }
        }

        #region Private Helper Methods

        private async Task<double> CalculateAverageProcessingTimeAsync(int adminId)
        {
            try
            {
                var avgDays = await _context.PermitApplications
                    .Where(p => p.AdminId == adminId &&
                               p.SubmissionDate != null &&
                               p.AdminApprovalDate.HasValue)
                    .Select(p => (double?)EF.Functions.DateDiffDay(p.SubmissionDate, p.AdminApprovalDate!.Value))
                    .AverageAsync();

                return avgDays ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average processing time for admin {AdminId}", adminId);
                return 0;
            }
        }

        #endregion
    }
}