using SocialPicture.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocialPicture.Application.Interfaces
{
    public interface ISavedImageService
    {
        Task<IEnumerable<ImageDto>> GetSavedImagesByUserIdAsync(int userId);
        Task<SavedImageDto> SaveImageAsync(int userId, int imageId);
        Task<bool> UnsaveImageAsync(int userId, int imageId);
        Task<bool> HasUserSavedImageAsync(int userId, int imageId);
    }
}
