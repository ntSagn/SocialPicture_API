using SocialPicture.Application.DTOs;

namespace SocialPicture.Application.Interfaces
{
    public interface ITagService
    {
        Task<IEnumerable<TagDto>> GetAllTagsAsync();
        Task<TagDto> GetTagByIdAsync(int id);
        Task<TagDto> GetTagByNameAsync(string name);
        Task<TagDto> CreateTagAsync(CreateTagDto createTagDto);
        Task<bool> DeleteTagAsync(int id);
        Task<IEnumerable<ImageDto>> GetImagesByTagIdAsync(int tagId, int? currentUserId = null);
        Task<IEnumerable<ImageDto>> GetImagesByTagNameAsync(string tagName, int? currentUserId = null);
        Task<bool> AddTagToImageAsync(int imageId, int tagId);
        Task<bool> RemoveTagFromImageAsync(int imageId, int tagId);
        Task<IEnumerable<TagDto>> GetTagsByImageIdAsync(int imageId);
        Task<IEnumerable<TagDto>> GetPopularTagsAsync(int count = 10);
    }
}

