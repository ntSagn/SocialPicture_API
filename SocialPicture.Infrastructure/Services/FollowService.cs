using Microsoft.EntityFrameworkCore;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using SocialPicture.Domain.Entities;
using SocialPicture.Persistence;

namespace SocialPicture.Infrastructure.Services
{
    public class FollowService : IFollowService
    {
        private readonly ApplicationDbContext _context;

        public FollowService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FollowerDto>> GetFollowersByUserIdAsync(int userId, int? currentUserId = null)
        {
            // Check if user exists
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
            if (!userExists)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            var followers = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.FollowingId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FollowerDto
                {
                    UserId = f.FollowerId,
                    Username = f.Follower.Username,
                    ProfilePicture = f.Follower.ProfilePicture,
                    FollowedAt = f.CreatedAt,
                    IsFollowedByCurrentUser = currentUserId.HasValue && 
                        _context.Follows.Any(followBack => followBack.FollowerId == currentUserId.Value && 
                                                          followBack.FollowingId == f.FollowerId)
                })
                .ToListAsync();

            return followers;
        }

        public async Task<IEnumerable<FollowingDto>> GetFollowingByUserIdAsync(int userId)
        {
            // Check if user exists
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
            if (!userExists)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            var following = await _context.Follows
                .Include(f => f.Following)
                .Where(f => f.FollowerId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FollowingDto
                {
                    UserId = f.FollowingId,
                    Username = f.Following.Username,
                    ProfilePicture = f.Following.ProfilePicture,
                    FollowingSince = f.CreatedAt
                })
                .ToListAsync();

            return following;
        }

        public async Task<bool> FollowUserAsync(int followerId, int followingId)
        {
            // Prevent self-following
            if (followerId == followingId)
            {
                throw new InvalidOperationException("Users cannot follow themselves.");
            }

            var following = await _context.Users.FindAsync(followingId);
            if (following != null)
            {
                following.FollowersCount = (following.FollowersCount ?? 0) + 1;
            }

            var follower = await _context.Users.FindAsync(followerId);
            if (follower != null)
            {
                follower.FollowingCount = (follower.FollowingCount ?? 0) + 1;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnfollowUserAsync(int followerId, int followingId)
        {
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (follow == null)
            {
                throw new KeyNotFoundException($"Follow relationship not found.");
            }

            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<bool> IsFollowingAsync(int followerId, int followingId)
        {
            return await _context.Follows
                .AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
        }

        public async Task<int> GetFollowersCountAsync(int userId)
        {
            return await _context.Follows.CountAsync(f => f.FollowingId == userId);
        }

        public async Task<int> GetFollowingCountAsync(int userId)
        {
            return await _context.Follows.CountAsync(f => f.FollowerId == userId);
        }
    }
}

