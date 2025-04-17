using Microsoft.EntityFrameworkCore;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using SocialPicture.Domain.Entities;
using SocialPicture.Domain.Enums;
using SocialPicture.Persistence;

namespace SocialPicture.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NotificationDto>> GetNotificationsByUserIdAsync(int userId, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var notificationDtos = new List<NotificationDto>();

            foreach (var n in notifications)
            {
                var senderProfilePicture = await GetSenderProfilePictureAsync(n.Type, n.ReferenceId);

                notificationDtos.Add(new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    UserId = n.UserId,
                    Type = n.Type,
                    ReferenceId = n.ReferenceId,
                    Content = n.Content,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    SenderProfilePicture = senderProfilePicture
                });
            }

            return notificationDtos;
        }

        public async Task<NotificationDto> GetNotificationByIdAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                throw new KeyNotFoundException($"Notification with ID {id} not found.");
            }

            var senderProfilePicture = await GetSenderProfilePictureAsync(notification.Type, notification.ReferenceId);

            return new NotificationDto
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                Type = notification.Type,
                ReferenceId = notification.ReferenceId,
                Content = notification.Content,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                SenderProfilePicture = senderProfilePicture
            };
        }

        public async Task<NotificationDto> CreateNotificationAsync(int userId, NotificationType type, int referenceId, string content)
        {
            // Check if user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                ReferenceId = referenceId,
                Content = content,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Immediately get the sender's profile picture for the response
            var senderProfilePicture = await GetSenderProfilePictureAsync(notification.Type, notification.ReferenceId);

            return new NotificationDto
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                Type = notification.Type,
                ReferenceId = notification.ReferenceId,
                Content = notification.Content,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                SenderProfilePicture = senderProfilePicture
            };
        }


        public async Task<bool> MarkNotificationAsReadAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                throw new KeyNotFoundException($"Notification with ID {id} not found.");
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<bool> MarkAllUserNotificationsAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!unreadNotifications.Any())
            {
                return true; // No notifications to mark
            }

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<int> GetUnreadNotificationsCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<NotificationSummaryDto> GetNotificationSummaryAsync(int userId)
        {
            var unreadCount = await GetUnreadNotificationsCountAsync(userId);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5) // Get 5 most recent notifications
                .ToListAsync();

            var recentNotifications = new List<NotificationDto>();

            foreach (var n in notifications)
            {
                var senderProfilePicture = await GetSenderProfilePictureAsync(n.Type, n.ReferenceId);

                recentNotifications.Add(new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    UserId = n.UserId,
                    Type = n.Type,
                    ReferenceId = n.ReferenceId,
                    Content = n.Content,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    SenderProfilePicture = senderProfilePicture
                });
            }

            return new NotificationSummaryDto
            {
                UnreadCount = unreadCount,
                RecentNotifications = recentNotifications
            };
        }

        public async Task<bool> DeleteNotificationAsync(int id)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id);

            if (notification == null)
            {
                throw new KeyNotFoundException($"Notification with ID {id} not found.");
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task<string?> GetSenderProfilePictureAsync(NotificationType type, int referenceId)
        {
            try
            {
                int senderId = 0;

                switch (type)
                {
                    case NotificationType.Like:
                        // For likes, referenceId is the imageId
                        // We need to get the user who performed the like action
                        var image = await _context.Images
                            .Include(i => i.Likes)
                            .ThenInclude(l => l.User)
                            .FirstOrDefaultAsync(i => i.ImageId == referenceId);

                        if (image != null && image.Likes.Any())
                        {
                            // Get the latest like's user profile picture
                            var latestLike = image.Likes.OrderByDescending(l => l.CreatedAt).FirstOrDefault();
                            return latestLike?.User?.ProfilePicture;
                        }
                        return null;

                    case NotificationType.Comment:
                        // For comments, referenceId is the commentId
                        var comment = await _context.Comments
                            .Include(c => c.User)
                            .FirstOrDefaultAsync(c => c.CommentId == referenceId);
                        return comment?.User?.ProfilePicture;

                    case NotificationType.CommentLike:
                        // For comment likes, referenceId might be the commentId
                        var commentLikes = await _context.CommentLikes
                            .Include(cl => cl.User)
                            .Where(cl => cl.CommentId == referenceId)
                            .OrderByDescending(cl => cl.CreatedAt)
                            .ToListAsync();

                        if (commentLikes.Any())
                        {
                            return commentLikes.First().User?.ProfilePicture;
                        }
                        return null;

                    case NotificationType.Follow:
                        // For follows, referenceId is the followerId (who initiated the follow)
                        var follower = await _context.Users
                            .FirstOrDefaultAsync(u => u.UserId == referenceId);
                        return follower?.ProfilePicture;

                    case NotificationType.ReportResolution:
                        var report = await _context.Reports
                            .Include(r => r.ResolvedBy)
                            .FirstOrDefaultAsync(r => r.ReportId == referenceId);
                        return report?.ResolvedBy?.ProfilePicture;

                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error fetching profile picture: {ex.Message}");
                return null;
            }
        }

    }
}
