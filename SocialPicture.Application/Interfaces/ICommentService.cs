using SocialPicture.Application.DTOs;

namespace SocialPicture.Application.Interfaces
{
    public interface ICommentService
    {
        Task<IEnumerable<CommentDto>> GetCommentsByImageIdAsync(int imageId, int? currentUserId = null);
        Task<CommentDto> GetCommentByIdAsync(int id, int? currentUserId = null);
        Task<CommentDto> CreateCommentAsync(int userId, CreateCommentDto createCommentDto);
        Task<CommentDto> UpdateCommentAsync(int id, int userId, UpdateCommentDto updateCommentDto);
        Task<bool> DeleteCommentAsync(int id, int userId);
        Task<IEnumerable<CommentDto>> GetRepliesByCommentIdAsync(int commentId, int? currentUserId = null);
    }
}

