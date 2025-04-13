using Microsoft.EntityFrameworkCore;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using SocialPicture.Domain.Entities;
using SocialPicture.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocialPicture.Infrastructure.Services
{
    public class SavedImageService : ISavedImageService
    {
        private readonly ApplicationDbContext _context;

        public SavedImageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ImageDto>> GetSavedImagesByUserIdAsync(int userId)
        {
            // Check if user exists
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
            if (!userExists)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            var savedImages = await _context.SavedImages
                .Where(s => s.UserId == userId)
                .Include(s => s.Image)
                    .ThenInclude(i => i.User)
                .Include(s => s.Image.Likes)
                .Include(s => s.Image.Comments)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new ImageDto
                {
                    ImageId = s.Image.ImageId,
                    UserId = s.Image.UserId,
                    UserName = s.Image.User.Username,
                    ImageUrl = s.Image.ImageUrl,
                    Caption = s.Image.Caption,
                    IsPublic = s.Image.IsPublic,
                    CreatedAt = s.Image.CreatedAt,
                    LikesCount = s.Image.Likes.Count,
                    CommentsCount = s.Image.Comments.Count,
                    IsLikedByCurrentUser = s.Image.Likes.Any(l => l.UserId == userId),
                    IsSavedByCurrentUser = true // Always true since these are saved images
                })
                .ToListAsync();

            return savedImages;
        }

        public async Task<SavedImageDto> SaveImageAsync(int userId, int imageId)
        {
            // Check if image exists
            var image = await _context.Images.FindAsync(imageId);
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

            // Check if user already saved the image
            var existingSave = await _context.SavedImages
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ImageId == imageId);

            if (existingSave != null)
            {
                throw new InvalidOperationException($"User already saved this image.");
            }

            // Create new saved image
            var savedImage = new SavedImage
            {
                UserId = userId,
                ImageId = imageId,
                CreatedAt = DateTime.UtcNow
            };

            _context.SavedImages.Add(savedImage);
            await _context.SaveChangesAsync();

            return new SavedImageDto
            {
                SavedImageId = savedImage.SavedImageId,
                UserId = savedImage.UserId,
                UserName = user.Username,
                ImageId = savedImage.ImageId,
                ImageUrl = image.ImageUrl,
                CreatedAt = savedImage.CreatedAt
            };
        }

        public async Task<bool> UnsaveImageAsync(int userId, int imageId)
        {
            var savedImage = await _context.SavedImages
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ImageId == imageId);

            if (savedImage == null)
            {
                throw new KeyNotFoundException($"Saved image not found for user {userId} on image {imageId}.");
            }

            _context.SavedImages.Remove(savedImage);
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<bool> HasUserSavedImageAsync(int userId, int imageId)
        {
            return await _context.SavedImages
                .AnyAsync(s => s.UserId == userId && s.ImageId == imageId);
        }
    }
}
