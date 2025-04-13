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
    public class LikeService : ILikeService
    {
        private readonly ApplicationDbContext _context;

        public LikeService(ApplicationDbContext context)
        {
            _context = context;
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
