using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;

namespace PerizinanPeternakan.Services
{
    public interface IAdminService
    {
        // Admin History Operations
        Task<(List<AdminHistoryViewModel> History, AdminHistoryStatsViewModel Stats, AdminHistoryPaginationViewModel Pagination)>
            GetAdminHistoryAsync(int adminId, DateTime? startDate, DateTime? endDate,
                PermitStatus? statusFilter, string searchTerm, int page, int pageSize = 10);

        // Admin Statistics
        Task<object> GetAdminStatisticsAsync(int adminId);
        Task<object> GetAdminActivitySummaryAsync(int adminId);
        Task<List<object>> GetAdminHistoryChartAsync(int adminId, int months = 6);

        // Admin History Management
        Task<(bool Success, string Message)> AddAdminHistoryAsync(
            int permitId, string action, string comments, int adminId);

        // Admin Test Operations (Development only)
        Task<List<object>> GetAvailablePermitsForTestAsync();
        (List<string> Results, bool Success) TestUploadDirectories(IWebHostEnvironment environment);
        Task<List<object>> CheckDocumentsAsync(IWebHostEnvironment environment, int? permitId = null);
        Task<(bool Success, List<string> Results)> CleanupOrphanedFilesAsync(IWebHostEnvironment environment);
    }

    public class AdminHistoryStatsViewModel
    {
        public string AdminName { get; set; } = "";
        public int TotalReviewed { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public int FilteredCount { get; set; }
    }

    public class AdminHistoryPaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public PermitStatus? StatusFilter { get; set; }
        public string? SearchTerm { get; set; }
    }
}