using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;

namespace PerizinanPeternakan.Services
{
    public interface IPermitService
    {
        // Permit CRUD Operations
        Task<List<PermitListViewModel>> GetPermitsForUserAsync(string userRole, int userId);
        Task<PermitDetailViewModel?> GetPermitDetailAsync(int permitId, string userRole, int userId);
        Task<(bool Success, string ErrorMessage, string ApplicationNumber)> CreatePermitAsync(
            PermitApplicationViewModel model, int userId);
        Task<string> GenerateApplicationNumberAsync();

        // Permit Status Operations
        Task<(bool Success, string ErrorMessage)> ApprovePermitAsync(
            int permitId, string action, string comments, int userId, string userRole);
        bool CanUserApprove(string userRole, PermitStatus status);

        // Document Operations
        Task<(bool Success, string ErrorMessage)> GeneratePermitDocumentAsync(int permitId);
        Task<string> GetDocumentPathAsync(int permitId);

        // Statistics and Analytics
        Task<object> GetPermitStatisticsAsync(string userRole, int userId);
        Task<object> GetDashboardDataAsync(string userRole, int userId);
        Task<List<object>> GetMonthlyTrendAsync(string userRole, int userId, int months = 6);

        // Search and Filter
        Task<(List<object> Data, int TotalCount)> GetPermitsDataAsync(
            string userRole, int userId, int start, int length, string search,
            string statusFilter, string dateFilter);
        Task<List<object>> AdvancedSearchAsync(AdvancedSearchRequest request, string userRole, int userId);

        // Bulk Operations
        Task<(bool Success, string Message, int SuccessCount, List<string> Errors)> BulkApproveAsync(
            List<int> permitIds, string comments, int userId, string userRole);
        Task<(bool Success, string Message, int SuccessCount, List<string> Errors)> BulkRejectAsync(
            List<int> permitIds, string comments, int userId, string userRole);

        // Export Operations
        Task<byte[]> ExportToCsvAsync(string userRole, int userId, string statusFilter,
            string dateFrom, string dateTo, string search);

        // Validation
        (bool IsValid, List<string> Errors) ValidatePermitApplication(PermitApplicationViewModel model);
        (bool IsValid, List<string> Errors) ValidateRequiredDocuments(PermitApplicationViewModel model);
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
}