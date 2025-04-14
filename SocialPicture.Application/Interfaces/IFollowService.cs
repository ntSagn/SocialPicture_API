using SocialPicture.Application.DTOs;

namespace SocialPicture.Application.Interfaces
{
    public interface IFollowService
    {
        Task<IEnumerable<FollowerDto>> GetFollowersByUserIdAsync(int userId, int? currentUserId = null);
        Task<IEnumerable<FollowingDto>> GetFollowingByUserIdAsync(int userId);
        Task<bool> FollowUserAsync(int followerId, int followingId);
        Task<bool> UnfollowUserAsync(int followerId, int followingId);
        Task<bool> IsFollowingAsync(int followerId, int followingId);
        Task<int> GetFollowersCountAsync(int userId);
        Task<int> GetFollowingCountAsync(int userId);
    }
}

