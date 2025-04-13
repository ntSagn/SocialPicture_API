using Microsoft.EntityFrameworkCore;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using SocialPicture.Domain.Entities;
using SocialPicture.Persistence;

namespace SocialPicture.Infrastructure.Services
{
    public class TagService : ITagService
    {
        private readonly ApplicationDbContext _context;

        public TagService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TagDto>> GetAllTagsAsync()
        {
            return await _context.Tags
                .Select(t => new TagDto
                {
                    TagId = t.TagId,
                    Name = t.Name,
                    CreatedAt = t.CreatedAt,
                    ImagesCount = t.ImageTags.Count
                })
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<TagDto> GetTagByIdAsync(int id)
        {
            var tag = await _context.Tags
                .Include(t => t.ImageTags)
                .FirstOrDefaultAsync(t => t.TagId == id);

            if (tag == null)
            {
                throw new KeyNotFoundException($"Tag with ID {id} not found.");
            }

            return new TagDto
            {
                TagId = tag.TagId,
                Name = tag.Name,
                CreatedAt = tag.CreatedAt,
                ImagesCount = tag.ImageTags.Count
            };
        }

        public async Task<TagDto> GetTagByNameAsync(string name)
        {
            var tag = await _context.Tags
                .Include(t => t.ImageTags)
                .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

            if (tag == null)
            {
                throw new KeyNotFoundException($"Tag with name '{name}' not found.");
            }

            return new TagDto
            {
                TagId = tag.TagId,
                Name = tag.Name,
                CreatedAt = tag.CreatedAt,
                ImagesCount = tag.ImageTags.Count
            };
        }

        public async Task<TagDto> CreateTagAsync(CreateTagDto createTagDto)
        {
            // Normalize tag name (lowercase, trim)
            var normalizedName = createTagDto.Name.Trim().ToLower();
            
            // Check if tag already exists
            var existingTag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedName);

            if (existingTag != null)
            {
                return new TagDto
                {
                    TagId = existingTag.TagId,
                    Name = existingTag.Name,
                    CreatedAt = existingTag.CreatedAt,
                    ImagesCount = existingTag.ImageTags.Count
                };
            }

            // Create new tag
            var tag = new Tag
            {
                Name = normalizedName,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            return new TagDto
            {
                TagId = tag.TagId,
                Name = tag.Name,
                CreatedAt = tag.CreatedAt,
                ImagesCount = 0
            };
        }

        public async Task<bool> DeleteTagAsync(int id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
            {
                throw new KeyNotFoundException($"Tag with ID {id} not found.");
            }

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<IEnumerable<ImageDto>> GetImagesByTagIdAsync(int tagId, int? currentUserId = null)
        {
            // Check if tag exists
            var tag = await _context.Tags.FindAsync(tagId);
            if (tag == null)
            {
                throw new KeyNotFoundException($"Tag with ID {tagId} not found.");
            }

            var images = await _context.ImageTags
                .Where(it => it.TagId == tagId)
                .Include(it => it.Image)
                    .ThenInclude(i => i.User)
                .Include(it => it.Image.Likes)
                .Include(it => it.Image.Comments)
                .Where(it => it.Image.IsPublic)
                .OrderByDescending(it => it.Image.CreatedAt)
                .Select(it => new ImageDto
                {
                    ImageId = it.Image.ImageId,
                    UserId = it.Image.UserId,
                    UserName = it.Image.User.Username,
                    ImageUrl = it.Image.ImageUrl,
                    Caption = it.Image.Caption,
                    IsPublic = it.Image.IsPublic,
                    CreatedAt = it.Image.CreatedAt,
                    LikesCount = it.Image.Likes.Count,
                    CommentsCount = it.Image.Comments.Count,
                    IsLikedByCurrentUser = currentUserId.HasValue && 
                        it.Image.Likes.Any(l => l.UserId == currentUserId.Value)
                })
                .ToListAsync();

            return images;
        }

        public async Task<IEnumerable<ImageDto>> GetImagesByTagNameAsync(string tagName, int? currentUserId = null)
        {
            // Normalize tag name
            var normalizedName = tagName.Trim().ToLower();
            
            // Check if tag exists
            var tag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedName);
                
            if (tag == null)
            {
                throw new KeyNotFoundException($"Tag with name '{tagName}' not found.");
            }

            return await GetImagesByTagIdAsync(tag.TagId, currentUserId);
        }

        public async Task<bool> AddTagToImageAsync(int imageId, int tagId)
        {
            // Check if image exists
            var image = await _context.Images.FindAsync(imageId);
            if (image == null)
            {
                throw new KeyNotFoundException($"Image with ID {imageId} not found.");
            }

            // Check if tag exists
            var tag = await _context.Tags.FindAsync(tagId);
            if (tag == null)
            {
                throw new KeyNotFoundException($"Tag with ID {tagId} not found.");
            }

            // Check if the relation already exists
            var existingImageTag = await _context.ImageTags
                .FirstOrDefaultAsync(it => it.ImageId == imageId && it.TagId == tagId);

            if (existingImageTag != null)
            {
                // Already exists, nothing to do
                return true;
            }

            // Create new relation
            var imageTag = new ImageTag
            {
                ImageId = imageId,
                TagId = tagId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ImageTags.Add(imageTag);
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<bool> RemoveTagFromImageAsync(int imageId, int tagId)
        {
            var imageTag = await _context.ImageTags
                .FirstOrDefaultAsync(it => it.ImageId == imageId && it.TagId == tagId);

            if (imageTag == null)
            {
                throw new KeyNotFoundException($"Image-Tag relation not found.");
            }

            _context.ImageTags.Remove(imageTag);
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<IEnumerable<TagDto>> GetTagsByImageIdAsync(int imageId)
        {
            // Check if image exists
            var image = await _context.Images.FindAsync(imageId);
            if (image == null)
            {
                throw new KeyNotFoundException($"Image with ID {imageId} not found.");
            }

            var tags = await _context.ImageTags
                .Where(it => it.ImageId == imageId)
                .Include(it => it.Tag)
                .Select(it => new TagDto
                {
                    TagId = it.Tag.TagId,
                    Name = it.Tag.Name,
                    CreatedAt = it.Tag.CreatedAt,
                    ImagesCount = it.Tag.ImageTags.Count
                })
                .OrderBy(t => t.Name)
                .ToListAsync();

            return tags;
        }

        public async Task<IEnumerable<TagDto>> GetPopularTagsAsync(int count = 10)
        {
            return await _context.Tags
                .Include(t => t.ImageTags)
                .OrderByDescending(t => t.ImageTags.Count)
                .Take(count)
                .Select(t => new TagDto
                {
                    TagId = t.TagId,
                    Name = t.Name,
                    CreatedAt = t.CreatedAt,
                    ImagesCount = t.ImageTags.Count
                })
                .ToListAsync();
        }
    }
}

