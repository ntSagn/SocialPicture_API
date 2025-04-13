using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using SocialPicture.Domain.Entities;
using SocialPicture.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SocialPicture.Infrastructure.Services
{
    public class ImageService : IImageService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _uploadDirectory;

        public ImageService(ApplicationDbContext context)
        {
            _context = context;
            // In a real app, get this from configuration
            _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
        }

        public async Task<IEnumerable<ImageDto>> GetAllImagesAsync(int? userId = null, bool publicOnly = true, int? currentUserId = null)
        {
            var query = _context.Images
                .Include(i => i.User)
                .Include(i => i.Likes)
                .Include(i => i.Comments)
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(i => i.UserId == userId);
            }

            if (publicOnly)
            {
                query = query.Where(i => i.IsPublic);
            }

            var images = await query
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var imageDtos = new List<ImageDto>();

            foreach (var image in images)
            {
                bool isLikedByCurrentUser = false;
                bool isSavedByCurrentUser = false;

                if (currentUserId.HasValue)
                {
                    isLikedByCurrentUser = await _context.Likes
                        .AnyAsync(l => l.UserId == currentUserId.Value && l.ImageId == image.ImageId);

                    isSavedByCurrentUser = await _context.SavedImages
                        .AnyAsync(s => s.UserId == currentUserId.Value && s.ImageId == image.ImageId);
                }

                imageDtos.Add(new ImageDto
                {
                    ImageId = image.ImageId,
                    UserId = image.UserId,
                    UserName = image.User.Username,
                    ImageUrl = image.ImageUrl,
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


        public async Task<ImageDto> GetImageByIdAsync(int id, int? currentUserId = null)
        {
            var image = await _context.Images
        .Include(i => i.User)
        .Include(i => i.Likes)
        .Include(i => i.Comments)
        .FirstOrDefaultAsync(i => i.ImageId == id);

            if (image == null)
            {
                throw new KeyNotFoundException($"Image with ID {id} not found.");
            }

            bool isLikedByCurrentUser = false;
            bool isSavedByCurrentUser = false;

            if (currentUserId.HasValue)
            {
                isLikedByCurrentUser = await _context.Likes
                    .AnyAsync(l => l.UserId == currentUserId.Value && l.ImageId == id);

                isSavedByCurrentUser = await _context.SavedImages
                    .AnyAsync(s => s.UserId == currentUserId.Value && s.ImageId == id);
            }

            return new ImageDto
            {
                ImageId = image.ImageId,
                UserId = image.UserId,
                UserName = image.User.Username,
                ImageUrl = image.ImageUrl,
                Caption = image.Caption,
                IsPublic = image.IsPublic,
                CreatedAt = image.CreatedAt,
                LikesCount = image.Likes.Count,
                CommentsCount = image.Comments.Count,
                IsLikedByCurrentUser = isLikedByCurrentUser,
                IsSavedByCurrentUser = isSavedByCurrentUser
            };
        }

        public async Task<ImageDto> CreateImageAsync(int userId, CreateImageDto createImageDto, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                throw new ArgumentException("No image file provided.");
            }

            // Check if user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
            var filePath = Path.Combine(_uploadDirectory, fileName);
            
            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // Create image record
            var image = new Image
            {
                UserId = userId,
                ImageUrl = $"/images/{fileName}", // Relative URL for storage
                Caption = createImageDto.Caption,
                IsPublic = createImageDto.IsPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Images.Add(image);
            await _context.SaveChangesAsync();

            return new ImageDto
            {
                ImageId = image.ImageId,
                UserId = image.UserId,
                UserName = user.Username,
                ImageUrl = image.ImageUrl,
                Caption = image.Caption,
                IsPublic = image.IsPublic,
                CreatedAt = image.CreatedAt,
                LikesCount = 0,
                CommentsCount = 0,
                IsLikedByCurrentUser = false
            };
        }

        public async Task<ImageDto> UpdateImageAsync(int id, int userId, UpdateImageDto updateImageDto)
        {
            var image = await _context.Images
                .Include(i => i.User)
                .Include(i => i.Likes)
                .Include(i => i.Comments)
                .FirstOrDefaultAsync(i => i.ImageId == id);

            if (image == null)
            {
                throw new KeyNotFoundException($"Image with ID {id} not found.");
            }

            if (image.UserId != userId)
            {
                throw new UnauthorizedAccessException("You don't have permission to update this image.");
            }

            if (updateImageDto.Caption != null)
            {
                image.Caption = updateImageDto.Caption;
            }

            if (updateImageDto.IsPublic.HasValue)
            {
                image.IsPublic = updateImageDto.IsPublic.Value;
            }

            image.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new ImageDto
            {
                ImageId = image.ImageId,
                UserId = image.UserId,
                UserName = image.User.Username,
                ImageUrl = image.ImageUrl,
                Caption = image.Caption,
                IsPublic = image.IsPublic,
                CreatedAt = image.CreatedAt,
                LikesCount = image.Likes.Count,
                CommentsCount = image.Comments.Count,
                IsLikedByCurrentUser = false // Will be updated by controller
            };
        }

        public async Task<bool> DeleteImageAsync(int id, int userId)
        {
            var image = await _context.Images.FindAsync(id);
            if (image == null)
            {
                throw new KeyNotFoundException($"Image with ID {id} not found.");
            }

            if (image.UserId != userId)
            {
                throw new UnauthorizedAccessException("You don't have permission to delete this image.");
            }

            // Delete the physical file if it exists
            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                var fileName = Path.GetFileName(image.ImageUrl);
                var filePath = Path.Combine(_uploadDirectory, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            _context.Images.Remove(image);
            await _context.SaveChangesAsync();
            
            return true;
        }
    }
}
