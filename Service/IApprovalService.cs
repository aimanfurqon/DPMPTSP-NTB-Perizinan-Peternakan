using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;

namespace PerizinanPeternakan.Services
{
    public interface IApprovalService
    {
        /// <summary>
        /// Process single approval/rejection
        /// </summary>
        /// <param name="permitId">Permit application ID</param>
        /// <param name="action">Approve or Reject</param>
        /// <param name="comments">Approval comments</param>
        /// <param name="userId">User performing the action</param>
        /// <param name="userRole">User role</param>
        /// <returns>Approval result</returns>
        Task<ApprovalResult> ProcessApprovalAsync(int permitId, string action, string comments, int userId, string userRole);

        /// <summary>
        /// Process bulk approvals
        /// </summary>
        /// <param name="permitIds">List of permit IDs</param>
        /// <param name="action">Approve or Reject</param>
        /// <param name="comments">Bulk comments</param>
        /// <param name="userId">User performing the action</param>
        /// <param name="userRole">User role</param>
        /// <returns>Bulk approval result</returns>
        Task<BulkApprovalResult> ProcessBulkApprovalAsync(List<int> permitIds, string action, string comments, int userId, string userRole);

        /// <summary>
        /// Process approval with data edits (Admin only)
        /// </summary>
        /// <param name="permitId">Permit application ID</param>
        /// <param name="model">View model with edits</param>
        /// <param name="userId">Admin user ID</param>
        /// <returns>Approval result with edit summary</returns>
        Task<ApprovalWithEditResult> ProcessApprovalWithEditsAsync(int permitId, PermitApprovalViewModel model, int userId);

        /// <summary>
        /// Check if user can approve permit at current status
        /// </summary>
        /// <param name="userRole">User role</param>
        /// <param name="permitStatus">Current permit status</param>
        /// <returns>True if can approve</returns>
        bool CanUserApprove(string userRole, PermitStatus permitStatus);

        /// <summary>
        /// Get permits available for approval by user role
        /// </summary>
        /// <param name="userRole">User role</param>
        /// <param name="userId">User ID (for user role)</param>
        /// <returns>List of permits pending approval</returns>
        Task<List<LivestockPermitApplication>> GetPendingApprovalsAsync(string userRole, int? userId = null);

        /// <summary>
        /// Get approval history for permit
        /// </summary>
        /// <param name="permitId">Permit application ID</param>
        /// <returns>List of approval history</returns>
        Task<List<PermitApprovalHistory>> GetApprovalHistoryAsync(int permitId);

        /// <summary>
        /// Calculate approval statistics for user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="userRole">User role</param>
        /// <returns>Approval statistics</returns>
        Task<ApprovalStatistics> GetApprovalStatisticsAsync(int userId, string userRole);

        /// <summary>
        /// Get approval timeline for permit
        /// </summary>
        /// <param name="permitId">Permit application ID</param>
        /// <returns>Timeline data</returns>
        Task<List<ApprovalTimelineItem>> GetApprovalTimelineAsync(int permitId);

        /// <summary>
        /// Validate permit before approval
        /// </summary>
        /// <param name="permitId">Permit application ID</param>
        /// <param name="userRole">User role performing approval</param>
        /// <returns>Validation result</returns>
        Task<ApprovalValidationResult> ValidateBeforeApprovalAsync(int permitId, string userRole);

        /// <summary>
        /// Auto-advance permit to next approval level if conditions are met
        /// </summary>
        /// <param name="permitId">Permit application ID</param>
        /// <returns>Auto-advance result</returns>
        Task<AutoAdvanceResult> AutoAdvancePermitAsync(int permitId);
        Task SendNewPermitNotificationsAsync(int permitId);
    }
}