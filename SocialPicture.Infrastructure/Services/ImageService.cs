using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ImageService(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;

            // Setup upload directory in wwwroot/images
            _uploadDirectory = Path.Combine(_environment.WebRootPath, "images");

            // Create directory if it doesn't exist
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
        }

        private string GetFullImageUrl(string relativeUrl)
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
                    UserProfilePicture = GetFullImageUrl(image.User.ProfilePicture), // Add profile picture
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

        public async Task<IEnumerable<ImageDto>> GetAllImagesAsync(
            int? userId = null,
            bool publicOnly = true,
            int? currentUserId = null,
            int page = 1,
            int pageSize = 20,
            bool personalizedFeed = false)
        {
            var query = _context.Images
                .Include(i => i.User)
                .Include(i => i.Likes)
                .Include(i => i.Comments)
                .AsQueryable();

            // Filter by user if specified
            if (userId.HasValue)
            {
                query = query.Where(i => i.UserId == userId);
            }

            // Always filter by public status if required
            if (publicOnly)
            {
                query = query.Where(i => i.IsPublic);
            }
            // If not public only but user is authenticated, allow them to see their own private images
            else if (currentUserId.HasValue)
            {
                query = query.Where(i => i.IsPublic || i.UserId == currentUserId.Value);
            }

            // Personalized feed logic
            if (personalizedFeed && currentUserId.HasValue)
            {
                // Get users followed by current user
                var followedUserIds = await _context.Follows
                    .Where(f => f.FollowerId == currentUserId.Value)
                    .Select(f => f.FollowingId)
                    .ToListAsync();

                // Prioritize images from followed users
                if (followedUserIds.Any())
                {
                    query = query.OrderByDescending(i => followedUserIds.Contains(i.UserId))
                                 .ThenByDescending(i => i.CreatedAt);
                }
                else
                {
                    query = query.OrderByDescending(i => i.CreatedAt);
                }
            }
            else
            {
                // Default ordering by creation date
                query = query.OrderByDescending(i => i.CreatedAt);
            }

            // Apply pagination
            var pagedImages = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var imageDtos = new List<ImageDto>();

            foreach (var image in pagedImages)
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
                    UserProfilePicture = GetFullImageUrl(image.User.ProfilePicture), // Add profile picture
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
                UserProfilePicture = GetFullImageUrl(image.User.ProfilePicture), // Add profile picture
                ImageUrl = GetFullImageUrl(image.ImageUrl),
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

            if (user != null)
            {
                user.PostsCount = (user.PostsCount ?? 0) + 1;
                await _context.SaveChangesAsync();
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
            var filePath = Path.Combine(_uploadDirectory, fileName);

            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // Store relative path in database - this should be correctly formatted for web access
            string relativePath = $"/images/{fileName}";

            // Create image record
            var image = new Image
            {
                UserId = userId,
                ImageUrl = relativePath, // Store relative URL in database
                Caption = createImageDto.Caption,
                IsPublic = createImageDto.IsPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Images.Add(image);
            await _context.SaveChangesAsync();

            // Return full URL in the DTO
            return new ImageDto
            {
                ImageId = image.ImageId,
                UserId = image.UserId,
                UserName = user.Username,
                UserProfilePicture = GetFullImageUrl(user.ProfilePicture), // Add profile picture
                ImageUrl = GetFullImageUrl(relativePath),
                Caption = image.Caption,
                IsPublic = image.IsPublic,
                CreatedAt = image.CreatedAt,
                LikesCount = 0,
                CommentsCount = 0,
                IsLikedByCurrentUser = false,
                IsSavedByCurrentUser = false
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
                UserProfilePicture = GetFullImageUrl(image.User.ProfilePicture), // Add profile picture
                ImageUrl = GetFullImageUrl(image.ImageUrl),
                Caption = image.Caption,
                IsPublic = image.IsPublic,
                CreatedAt = image.CreatedAt,
                LikesCount = image.Likes.Count,
                CommentsCount = image.Comments.Count,
                IsLikedByCurrentUser = false,
                IsSavedByCurrentUser = false
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

            var savedImages = await _context.SavedImages
                .Where(s => s.ImageId == id)
                .ToListAsync();
            if (savedImages.Any())
            {
                _context.SavedImages.RemoveRange(savedImages);
            }

            var imageLikes = await _context.Likes
                .Where(l => l.ImageId == id)
                .ToListAsync();
            if (imageLikes.Any())
            {
                _context.Likes.RemoveRange(imageLikes);
            }

            
            var comments = await _context.Comments
                .Where(c => c.ImageId == id)
                .ToListAsync();
            if (comments.Any())
            {
                _context.Comments.RemoveRange(comments);
            }

            
            var imageTags = await _context.ImageTags
                .Where(t => t.ImageId == id)
                .ToListAsync();
            if (imageTags.Any())
            {
                _context.ImageTags.RemoveRange(imageTags);
            }

            
            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                try
                {
                    
                    string fileName;
                    if (image.ImageUrl.Contains("/images/"))
                    {
                        fileName = image.ImageUrl.Substring(image.ImageUrl.LastIndexOf("/") + 1);
                    }
                    else
                    {
                        fileName = Path.GetFileName(image.ImageUrl);
                    }

                    var filePath = Path.Combine(_uploadDirectory, fileName);
                    Console.WriteLine($"Attempting to delete file at path: {filePath}");

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Console.WriteLine($"Successfully deleted file: {filePath}");
                    }
                    else
                    {
                        Console.WriteLine($"File not found: {filePath}");
                    }
                }
                catch (Exception ex)
                {
                    
                    Console.WriteLine($"Error deleting image file: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }

            
            _context.Images.Remove(image);
            await _context.SaveChangesAsync();

           
            var user = await _context.Users.FindAsync(image.UserId);
            if (user != null && user.PostsCount.HasValue && user.PostsCount > 0)
            {
                user.PostsCount--;
                await _context.SaveChangesAsync();
            }

            return true;
        }

    }
}
