using Microsoft.EntityFrameworkCore;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using SocialPicture.Domain.Entities;
using SocialPicture.Domain.Enums;
using SocialPicture.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocialPicture.Infrastructure.Services
{
    public class CommentLikeService : ICommentLikeService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public CommentLikeService(
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<UserDto>> GetLikesByCommentIdAsync(int commentId)
        {
            // Check if comment exists
            var commentExists = await _context.Comments.AnyAsync(c => c.CommentId == commentId);
            if (!commentExists)
            {
                throw new KeyNotFoundException($"Comment with ID {commentId} not found.");
            }

            var users = await _context.CommentLikes
                .Where(cl => cl.CommentId == commentId)
                .Include(cl => cl.User)
                .OrderByDescending(cl => cl.CreatedAt)
                .Select(cl => new UserDto
                {
                    UserId = cl.User.UserId,
                    Username = cl.User.Username,
                    Email = cl.User.Email,
                    Fullname = cl.User.Fullname,
                    Bio = cl.User.Bio,
                    ProfilePicture = cl.User.ProfilePicture,
                    Role = cl.User.Role,
                    CreatedAt = cl.User.CreatedAt
                })
                .ToListAsync();

            return users;
        }

        public async Task<CommentLikeDto> LikeCommentAsync(int userId, int commentId)
        {
            // Check if comment exists
            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null)
            {
                throw new KeyNotFoundException($"Comment with ID {commentId} not found.");
            }

            // Check if user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            // Check if user already liked the comment
            var existingLike = await _context.CommentLikes
                .FirstOrDefaultAsync(cl => cl.UserId == userId && cl.CommentId == commentId);

            if (existingLike != null)
            {
                throw new InvalidOperationException($"User already liked this comment.");
            }

            // Create new like
            var commentLike = new CommentLike
            {
                UserId = userId,
                CommentId = commentId,
                CreatedAt = DateTime.Now
            };

            _context.CommentLikes.Add(commentLike);
            await _context.SaveChangesAsync();

            // Create notification if the comment owner is not the same user who liked
            if (comment.UserId != userId)
            {
                string content = $"{user.Username} liked your comment.";
                await _notificationService.CreateNotificationAsync(
                    comment.UserId,
                    NotificationType.Like,
                    commentId,
                    content);
            }

            return new CommentLikeDto
            {
                CommentLikeId = commentLike.CommentLikeId,
                UserId = commentLike.UserId,
                Username = user.Username,
                CommentId = commentLike.CommentId,
                CreatedAt = commentLike.CreatedAt
            };
        }

        public async Task<bool> UnlikeCommentAsync(int userId, int commentId)
        {
            var commentLike = await _context.CommentLikes
                .FirstOrDefaultAsync(cl => cl.UserId == userId && cl.CommentId == commentId);

            if (commentLike == null)
            {
                throw new KeyNotFoundException($"Like not found for user {userId} on comment {commentId}.");
            }

            _context.CommentLikes.Remove(commentLike);
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<bool> HasUserLikedCommentAsync(int userId, int commentId)
        {
            return await _context.CommentLikes
                .AnyAsync(cl => cl.UserId == userId && cl.CommentId == commentId);
        }

        public async Task<int> GetLikesCountByCommentIdAsync(int commentId)
        {
            return await _context.CommentLikes
                .CountAsync(cl => cl.CommentId == commentId);
        }
    }
}
