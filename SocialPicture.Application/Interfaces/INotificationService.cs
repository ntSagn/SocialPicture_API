using SocialPicture.Application.DTOs;
using SocialPicture.Domain.Enums;

namespace SocialPicture.Application.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetNotificationsByUserIdAsync(int userId, bool unreadOnly = false);
        Task<NotificationDto> GetNotificationByIdAsync(int id);
        Task<NotificationDto> CreateNotificationAsync(int userId, NotificationType type, int referenceId, string content);
        Task<bool> MarkNotificationAsReadAsync(int id);
        Task<bool> MarkAllUserNotificationsAsReadAsync(int userId);
        Task<int> GetUnreadNotificationsCountAsync(int userId);
        Task<NotificationSummaryDto> GetNotificationSummaryAsync(int userId);
    }
}
