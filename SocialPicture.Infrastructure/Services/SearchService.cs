// SocialPicture.Infrastructure/Services/SearchService.cs
using Microsoft.EntityFrameworkCore;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using SocialPicture.Persistence;

namespace SocialPicture.Infrastructure.Services
{
    public class SearchService : ISearchService
    {
        private readonly ApplicationDbContext _context;

        public SearchService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ImageDto>> SearchImagesAsync(string query, int? currentUserId = null, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<ImageDto>();

            // Normalize query
            query = query.ToLower().Trim();

            var skip = (page - 1) * pageSize;

            // Search in image captions and tags
            var images = await _context.Images
                .Include(i => i.User)
                .Include(i => i.Likes)
                .Include(i => i.Comments)
                .Include(i => i.ImageTags)
                    .ThenInclude(it => it.Tag)
                .Where(i => i.IsPublic && 
                           (i.Caption != null && i.Caption.ToLower().Contains(query) ||
                            i.ImageTags.Any(t => t.Tag.Name.ToLower().Contains(query))))
                .OrderByDescending(i => i.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return images.Select(i => new ImageDto
            {
                ImageId = i.ImageId,
                UserId = i.UserId,
                UserName = i.User.Username,
                ImageUrl = i.ImageUrl,
                Caption = i.Caption,
                IsPublic = i.IsPublic,
                CreatedAt = i.CreatedAt,
                LikesCount = i.Likes.Count,
                CommentsCount = i.Comments.Count,
                IsLikedByCurrentUser = currentUserId.HasValue && i.Likes.Any(l => l.UserId == currentUserId.Value),
                IsSavedByCurrentUser = currentUserId.HasValue && _context.SavedImages.Any(s => s.UserId == currentUserId.Value && s.ImageId == i.ImageId)
            });
        }
        public async Task<IEnumerable<UserDto>> SearchUsersAsync(string query, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<UserDto>();

            // Normalize query
            query = query.ToLower().Trim();

            var skip = (page - 1) * pageSize;

            // Search in username, fullname and bio
            var users = await _context.Users
                .Where(u => u.Username.ToLower().Contains(query) ||
                            u.Fullname.ToLower().Contains(query) ||
                            (u.Bio != null && u.Bio.ToLower().Contains(query)))
                .OrderBy(u => u.Username)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return users.Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Fullname = u.Fullname,
                ProfilePicture = u.ProfilePicture,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                // Use the properties directly from the User entity
                FollowersCount = u.FollowersCount ?? 0,
                FollowingCount = u.FollowingCount ?? 0,
                PostsCount = u.PostsCount ?? 0
            });
        }
    }
}
