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

            return notifications.Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                UserId = n.UserId,
                Type = n.Type,
                ReferenceId = n.ReferenceId,
                Content = n.Content,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });
        }

        public async Task<NotificationDto> GetNotificationByIdAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                throw new KeyNotFoundException($"Notification with ID {id} not found.");
            }

            return new NotificationDto
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                Type = notification.Type,
                ReferenceId = notification.ReferenceId,
                Content = notification.Content,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
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
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return new NotificationDto
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                Type = notification.Type,
                ReferenceId = notification.ReferenceId,
                Content = notification.Content,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
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
            
            var recentNotifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5) // Get 5 most recent notifications
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    UserId = n.UserId,
                    Type = n.Type,
                    ReferenceId = n.ReferenceId,
                    Content = n.Content,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return new NotificationSummaryDto
            {
                UnreadCount = unreadCount,
                RecentNotifications = recentNotifications
            };
        }
    }
}
