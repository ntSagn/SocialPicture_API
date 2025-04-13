using SocialPicture.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialPicture.Application.Interfaces
{
    public interface ILikeService
    {
        Task<IEnumerable<LikeDto>> GetLikesByImageIdAsync(int imageId);
        Task<LikeDto> LikeImageAsync(int userId, int imageId);
        Task<bool> UnlikeImageAsync(int userId, int imageId);
        Task<bool> HasUserLikedImageAsync(int userId, int imageId);
    }
}
