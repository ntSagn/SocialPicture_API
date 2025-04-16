using Microsoft.AspNetCore.Http;
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
using static System.Net.Mime.MediaTypeNames;

namespace SocialPicture.Infrastructure.Services
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        

        public CommentService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;    
        }

        private string GetFullImageUrl(string? relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl))
                return string.Empty;

            // If already a full URL, return as is
            if (relativeUrl.StartsWith("http://") || relativeUrl.StartsWith("https://"))
                return relativeUrl;

            // Build base URL from current request
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return relativeUrl;

            var baseUrl = $"{request.Scheme}://{request.Host}";

            // Remove leading slash if present for proper joining
            if (relativeUrl.StartsWith("/"))
                relativeUrl = relativeUrl.Substring(1);

            return $"{baseUrl}/{relativeUrl}";
        }

        public async Task<IEnumerable<CommentDto>> GetCommentsByImageIdAsync(int imageId, int? currentUserId = null)
        {
            // Check if image exists
            var imageExists = await _context.Images.AnyAsync(i => i.ImageId == imageId);
            if (!imageExists)
            {
                throw new KeyNotFoundException($"Image with ID {imageId} not found.");
            }

            // Get root comments (those without a parent)
            var comments = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.CommentLikes)
                .Where(c => c.ImageId == imageId && c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var commentDtos = new List<CommentDto>();

            foreach (var comment in comments)
            {
                var commentDto = await MapCommentToCommentDtoAsync(comment, currentUserId);
                commentDto.Replies = await GetRepliesRecursiveAsync(comment.CommentId, currentUserId);
                commentDtos.Add(commentDto);
            }

            return commentDtos;
        }

        private async Task<List<CommentDto>> GetRepliesRecursiveAsync(int parentCommentId, int? currentUserId = null)
        {
            var replies = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.CommentLikes)
                .Where(c => c.ParentCommentId == parentCommentId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            var replyDtos = new List<CommentDto>();

            foreach (var reply in replies)
            {
                var replyDto = await MapCommentToCommentDtoAsync(reply, currentUserId);
                replyDto.Replies = await GetRepliesRecursiveAsync(reply.CommentId, currentUserId);
                replyDtos.Add(replyDto);
            }

            return replyDtos;
        }

        public async Task<CommentDto> GetCommentByIdAsync(int id, int? currentUserId = null)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.CommentLikes)
                .FirstOrDefaultAsync(c => c.CommentId == id);

            if (comment == null)
            {
                throw new KeyNotFoundException($"Comment with ID {id} not found.");
            }

            var commentDto = await MapCommentToCommentDtoAsync(comment, currentUserId);

            if (comment.ParentCommentId == null)
            {
                commentDto.Replies = await GetRepliesRecursiveAsync(comment.CommentId, currentUserId);
            }

            return commentDto;
        }

        public async Task<CommentDto> CreateCommentAsync(int userId, CreateCommentDto createCommentDto)
        {
            // Check if user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            // Check if image exists
            var image = await _context.Images.FindAsync(createCommentDto.ImageId);
            if (image == null)
            {
                throw new KeyNotFoundException($"Image with ID {createCommentDto.ImageId} not found.");
            }

            // If it's a reply, check if parent comment exists
            if (createCommentDto.ParentCommentId.HasValue)
            {
                var parentComment = await _context.Comments.FindAsync(createCommentDto.ParentCommentId.Value);
                if (parentComment == null)
                {
                    throw new KeyNotFoundException($"Parent comment with ID {createCommentDto.ParentCommentId.Value} not found.");
                }

                // Ensure the parent comment belongs to the same image
                if (parentComment.ImageId != createCommentDto.ImageId)
                {
                    throw new InvalidOperationException("Parent comment does not belong to the specified image.");
                }
            }

            // Create the comment
            var comment = new Comment
            {
                UserId = userId,
                ImageId = createCommentDto.ImageId,
                Content = createCommentDto.Content,
                ParentCommentId = createCommentDto.ParentCommentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Reload the comment to get navigation properties
            comment = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.CommentLikes)
                .FirstOrDefaultAsync(c => c.CommentId == comment.CommentId);

            // Return the newly created comment mapped to DTO
            return await MapCommentToCommentDtoAsync(comment!, userId);
        }

        public async Task<CommentDto> UpdateCommentAsync(int id, int userId, UpdateCommentDto updateCommentDto)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.CommentLikes)
                .FirstOrDefaultAsync(c => c.CommentId == id);

            if (comment == null)
            {
                throw new KeyNotFoundException($"Comment with ID {id} not found.");
            }

            // Check if user is the owner of the comment
            if (comment.UserId != userId)
            {
                throw new UnauthorizedAccessException("You don't have permission to update this comment.");
            }

            // Update the comment
            comment.Content = updateCommentDto.Content;
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var commentDto = await MapCommentToCommentDtoAsync(comment, userId);

            if (comment.ParentCommentId == null)
            {
                commentDto.Replies = await GetRepliesRecursiveAsync(comment.CommentId, userId);
            }

            return commentDto;
        }

        public async Task<bool> DeleteCommentAsync(int id, int userId)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentId == id);

            if (comment == null)
            {
                throw new KeyNotFoundException($"Comment with ID {id} not found.");
            }

            // Check if user is the owner of the comment or is an admin
            var currentUser = await _context.Users.FindAsync(userId);
            if (comment.UserId != userId && currentUser?.Role != Domain.Enums.UserRole.ADMIN)
            {
                throw new UnauthorizedAccessException("You don't have permission to delete this comment.");
            }

            // Delete the comment and its replies will cascade due to FK constraints
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<CommentDto>> GetRepliesByCommentIdAsync(int commentId, int? currentUserId = null)
        {
            var parentComment = await _context.Comments.FindAsync(commentId);
            if (parentComment == null)
            {
                throw new KeyNotFoundException($"Parent comment with ID {commentId} not found.");
            }

            return await GetRepliesRecursiveAsync(commentId, currentUserId);
        }

        public async Task<CommentDto> MapCommentToCommentDtoAsync(Comment comment, int? currentUserId = null)
        {
            var likesCount = await _context.CommentLikes.CountAsync(cl => cl.CommentId == comment.CommentId);
            var repliesCount = await _context.Comments.CountAsync(c => c.ParentCommentId == comment.CommentId);

            bool isLikedByCurrentUser = false;
            if (currentUserId.HasValue)
            {
                isLikedByCurrentUser = await _context.CommentLikes
                    .AnyAsync(cl => cl.CommentId == comment.CommentId && cl.UserId == currentUserId.Value);
            }

            return new CommentDto
            {
                CommentId = comment.CommentId,
                UserId = comment.UserId,
                Username = comment.User?.Username ?? string.Empty,
                UserProfilePicture = GetFullImageUrl(comment.User?.ProfilePicture),
                ImageId = comment.ImageId,
                Content = comment.Content,
                ParentCommentId = comment.ParentCommentId,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                LikesCount = likesCount,
                IsLikedByCurrentUser = isLikedByCurrentUser,
                RepliesCount = repliesCount,
                Replies = new List<CommentDto>() // Will be populated separately for top-level comments
            };
        }
    }
}
