using SocialPicture.Domain.Enums;

namespace SocialPicture.Application.DTOs
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public NotificationType Type { get; set; }
        public int ReferenceId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationSummaryDto
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> RecentNotifications { get; set; } = new List<NotificationDto>();
    }
}
