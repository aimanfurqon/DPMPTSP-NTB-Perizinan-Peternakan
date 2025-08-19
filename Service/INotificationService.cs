using PerizinanPeternakan.Models;

namespace PerizinanPeternakan.Services
{
    /// <summary>
    /// Interface for sending notifications related to permit applications.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a notification for a new permit application.
        /// </summary>
        /// <param name="permit">The livestock permit application.</param>
        /// <param name="userRole">The role of the user.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendNewPermitNotificationAsync(LivestockPermitApplication permit, string userRole);
        
        /// <summary>
        /// Sends an approval notification for a permit application.
        /// </summary>
        /// <param name="permit">The livestock permit application.</param>
        /// <param name="action">The approval action taken.</param>
        /// <param name="comments">The comments for the approval.</param>
        /// <param name="userRole">The role of the user.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendApprovalNotificationAsync(LivestockPermitApplication permit, string action, string comments, string userRole);
        
        /// <summary>
        /// Sends a rejection notification for a permit application.
        /// </summary>
        /// <param name="permit">The livestock permit application.</param>
        /// <param name="comments">The rejection comments.</param>
        /// <param name="userRole">The role of the user.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendRejectionNotificationAsync(LivestockPermitApplication permit, string comments, string userRole);
        
        /// <summary>
        /// Sends a final approval notification for a permit application.
        /// </summary>
        /// <param name="permit">The livestock permit application.</param>
        /// <param name="comments">The approval comments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendFinalApprovalNotificationAsync(LivestockPermitApplication permit, string comments);
        
        /// <summary>
        /// Sends a rejection notification to the previous approval level.
        /// </summary>
        /// <param name="permit">The livestock permit application.</param>
        /// <param name="comments">The rejection comments.</param>
        /// <param name="userRole">The role of the user.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendRejectionToPreviousLevelAsync(LivestockPermitApplication permit, string comments, string userRole);
    }
} 