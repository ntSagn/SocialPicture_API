using SocialPicture.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocialPicture.Application.Interfaces
{
    public interface ICommentLikeService
    {
        Task<IEnumerable<UserDto>> GetLikesByCommentIdAsync(int commentId);
        Task<CommentLikeDto> LikeCommentAsync(int userId, int commentId);
        Task<bool> UnlikeCommentAsync(int userId, int commentId);
        Task<bool> HasUserLikedCommentAsync(int userId, int commentId);
        Task<int> GetLikesCountByCommentIdAsync(int commentId);
    }
}
