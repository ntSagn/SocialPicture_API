// SocialPicture.Application/Interfaces/ISearchService.cs
using SocialPicture.Application.DTOs;

namespace SocialPicture.Application.Interfaces
{
    public interface ISearchService
    {
        Task<IEnumerable<ImageDto>> SearchImagesAsync(string query, int? currentUserId = null, int page = 1, int pageSize = 10);
        Task<IEnumerable<UserDto>> SearchUsersAsync(string query, int page = 1, int pageSize = 10);
    }
}
