using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using System.Security.Claims;

namespace SocialPicture.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get all notifications for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetMyNotifications([FromQuery] bool unreadOnly = false)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId, unreadOnly);
            return Ok(notifications);
        }

        /// <summary>
        /// Get notification by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationDto>> GetNotificationById(int id)
        {
            try
            {
                var notification = await _notificationService.GetNotificationByIdAsync(id);
                
                // Ensure users can only see their own notifications
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (notification.UserId != userId)
                {
                    return Forbid();
                }
                
                return Ok(notification);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                // First, get the notification to verify ownership
                var notification = await _notificationService.GetNotificationByIdAsync(id);
                
                // Ensure users can only mark their own notifications as read
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (notification.UserId != userId)
                {
                    return Forbid();
                }
                
                await _notificationService.MarkNotificationAsReadAsync(id);
                return Ok(new { success = true });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Mark all notifications as read for the current user
        /// </summary>
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _notificationService.MarkAllUserNotificationsAsReadAsync(userId);
            return Ok(new { success = true });
        }

        /// <summary>
        /// Get count of unread notifications
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadNotificationsCount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var count = await _notificationService.GetUnreadNotificationsCountAsync(userId);
            return Ok(count);
        }

        /// <summary>
        /// Get notification summary (unread count and recent notifications)
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<NotificationSummaryDto>> GetNotificationSummary()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var summary = await _notificationService.GetNotificationSummaryAsync(userId);
            return Ok(summary);
        }
    }
}
