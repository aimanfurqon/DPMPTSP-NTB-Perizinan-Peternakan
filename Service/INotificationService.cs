using PerizinanPeternakan.Models;

namespace PerizinanPeternakan.Services
{
    public interface INotificationService
    {
        Task SendNewPermitNotificationAsync(LivestockPermitApplication permit, string userRole);
        Task SendApprovalNotificationAsync(LivestockPermitApplication permit, string action, string comments, string userRole);
        Task SendRejectionNotificationAsync(LivestockPermitApplication permit, string comments, string userRole);
    }
} 