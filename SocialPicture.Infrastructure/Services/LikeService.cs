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

namespace SocialPicture.Infrastructure.Services
{
    public class LikeService : ILikeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public LikeService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
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

        public async Task<IEnumerable<ImageDto>> GetLikedImagesByUserIdAsync(int userId, int? currentUserId = null)
        {
            // Check if user exists
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
            if (!userExists)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            // Get all images liked by the user
            var likedImages = await _context.Likes
                .Where(l => l.UserId == userId)
                .Include(l => l.Image)
                    .ThenInclude(i => i.User)
                .Include(l => l.Image.Likes)
                .Include(l => l.Image.Comments)
                .Include(l => l.Image.SavedByUsers)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => l.Image)
                .ToListAsync();

            // Map to DTOs
            var imageDtos = new List<ImageDto>();

            foreach (var image in likedImages)
            {
                bool isLikedByCurrentUser = false;
                bool isSavedByCurrentUser = false;

                if (currentUserId.HasValue)
                {
                    isLikedByCurrentUser = image.Likes.Any(l => l.UserId == currentUserId.Value);
                    isSavedByCurrentUser = image.SavedByUsers.Any(s => s.UserId == currentUserId.Value);
                }
                else if (currentUserId == userId)
                {
                    // If viewing own likes, they're all liked by the current user
                    isLikedByCurrentUser = true;
                }

                imageDtos.Add(new ImageDto
                {
                    ImageId = image.ImageId,
                    UserId = image.UserId,
                    UserName = image.User.Username,
                    UserProfilePicture = GetFullImageUrl(image.User.ProfilePicture),
                    ImageUrl = GetFullImageUrl(image.ImageUrl),
                    Caption = image.Caption,
                    IsPublic = image.IsPublic,
                    CreatedAt = image.CreatedAt,
                    LikesCount = image.Likes.Count,
                    CommentsCount = image.Comments.Count,
                    IsLikedByCurrentUser = isLikedByCurrentUser,
                    IsSavedByCurrentUser = isSavedByCurrentUser
                });
            }

            return imageDtos;
        }

        public async Task<IEnumerable<LikeDto>> GetLikesByImageIdAsync(int imageId)
        {
            // Check if image exists
            var imageExists = await _context.Images.AnyAsync(i => i.ImageId == imageId);
            if (!imageExists)
            {
                throw new KeyNotFoundException($"Image with ID {imageId} not found.");
            }

            var likes = await _context.Likes
                .Include(l => l.User)
                .Where(l => l.ImageId == imageId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return likes.Select(l => new LikeDto
            {
                LikeId = l.LikeId,
                UserId = l.UserId,
                Username = l.User.Username,
                ImageId = l.ImageId,
                CreatedAt = l.CreatedAt
            });
        }

        public async Task<LikeDto> LikeImageAsync(int userId, int imageId)
        {
            // Check if image exists
            var image = await _context.Images
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.ImageId == imageId);

            if (image == null)
            {
                throw new KeyNotFoundException($"Image with ID {imageId} not found.");
            }

            // Check if user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            // Check if user already liked the image
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.ImageId == imageId);

            if (existingLike != null)
            {
                throw new InvalidOperationException($"User already liked this image.");
            }

            // Create new like
            var like = new Like
            {
                UserId = userId,
                ImageId = imageId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            // Create notification if the image owner is not the same user who liked
            if (image.UserId != userId)
            {
                string content = $"{user.Username} liked your image.";
                await _notificationService.CreateNotificationAsync(
                    image.UserId,
                    NotificationType.Like,
                    imageId,
                    content);
            }

            return new LikeDto
            {
                LikeId = like.LikeId,
                UserId = like.UserId,
                Username = user.Username,
                ImageId = like.ImageId,
                CreatedAt = like.CreatedAt
            };
        }

        public async Task<bool> UnlikeImageAsync(int userId, int imageId)
        {
            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.ImageId == imageId);

            if (like == null)
            {
                throw new KeyNotFoundException($"Like not found for user {userId} on image {imageId}.");
            }

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> HasUserLikedImageAsync(int userId, int imageId)
        {
            return await _context.Likes
                .AnyAsync(l => l.UserId == userId && l.ImageId == imageId);
        }
    }
}
