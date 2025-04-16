using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using SocialPicture.Domain.Entities;
using SocialPicture.Domain.Enums;
using SocialPicture.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocialPicture.Infrastructure.Services
{
    public class FollowService : IFollowService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public FollowService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, INotificationService notificationService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        // Helper method to get full image URL
        private string GetFullImageUrl(string relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl))
                return null;

            if (relativeUrl.StartsWith("http"))
                return relativeUrl;

            var request = _httpContextAccessor.HttpContext.Request;
            return $"{request.Scheme}://{request.Host}{relativeUrl}";
        }

        public async Task<IEnumerable<FollowerDto>> GetFollowersByUserIdAsync(int userId, int? currentUserId = null)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            try
            {
                // Debug the query first
                var followRecords = await _context.Follows
                    .Where(f => f.FollowingId == userId)
                    .ToListAsync();

                // Now load the followers explicitly
                var followers = new List<FollowerDto>();

                foreach (var follow in followRecords)
                {
                    // Get the follower user separately to avoid navigation property issues
                    var follower = await _context.Users.FindAsync(follow.FollowerId);
                    if (follower != null)
                    {
                        bool isFollowedByCurrentUser = false;
                        if (currentUserId.HasValue)
                        {
                            isFollowedByCurrentUser = await _context.Follows
                                .AnyAsync(ff => ff.FollowerId == currentUserId && ff.FollowingId == follower.UserId);
                        }

                        followers.Add(new FollowerDto
                        {
                            UserId = follower.UserId,
                            Username = follower.Username,
                            Fullname = follower.Fullname,
                            ProfilePicture = GetFullImageUrl(follower.ProfilePicture),
                            IsFollowedByCurrentUser = isFollowedByCurrentUser,
                            FollowingSince = follow.CreatedAt
                        });
                    }
                }

                return followers;
            }
            catch (Exception ex)
            {
                // Add logging here
                throw new Exception($"Error retrieving followers: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<FollowingDto>> GetFollowingByUserIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            try
            {
                // Debug the query first
                var followRecords = await _context.Follows
                    .Where(f => f.FollowerId == userId)
                    .ToListAsync();

                // Now load the following users explicitly
                var following = new List<FollowingDto>();

                foreach (var follow in followRecords)
                {
                    // Get the following user separately to avoid navigation property issues
                    var followingUser = await _context.Users.FindAsync(follow.FollowingId);
                    if (followingUser != null)
                    {
                        following.Add(new FollowingDto
                        {
                            UserId = followingUser.UserId,
                            Username = followingUser.Username,
                            Fullname = followingUser.Fullname,
                            ProfilePicture = GetFullImageUrl(followingUser.ProfilePicture),
                            FollowingSince = follow.CreatedAt
                        });
                    }
                }

                return following;
            }
            catch (Exception ex)
            {
                // Add logging here
                throw new Exception($"Error retrieving following: {ex.Message}", ex);
            }
        }


        public async Task<bool> FollowUserAsync(int followerId, int followingId)
        {
            // Validate users exist
            var follower = await _context.Users.FindAsync(followerId);
            if (follower == null)
            {
                throw new KeyNotFoundException($"User with ID {followerId} not found.");
            }

            var following = await _context.Users.FindAsync(followingId);
            if (following == null)
            {
                throw new KeyNotFoundException($"User with ID {followingId} not found.");
            }

            // Check if already following
            var existingFollow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (existingFollow != null)
            {
                throw new InvalidOperationException("You are already following this user.");
            }

            // Prevent self-follow
            if (followerId == followingId)
            {
                throw new InvalidOperationException("You cannot follow yourself.");
            }

            // Create new follow
            var follow = new Follow
            {
                FollowerId = followerId,
                FollowingId = followingId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Follows.Add(follow);

            // Update user follower/following counts
            follower.FollowingCount = (follower.FollowingCount ?? 0) + 1;
            following.FollowersCount = (following.FollowersCount ?? 0) + 1;

            await _context.SaveChangesAsync();
            if (follower != null)
            {
                string content = $"{follower.Username} started following you.";
                await _notificationService.CreateNotificationAsync(
                    followingId,
                    NotificationType.Follow,
                    followerId,
                    content);
            }
            return true;
        }

        public async Task<bool> UnfollowUserAsync(int followerId, int followingId)
        {
            // Find the follow relationship
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (follow == null)
            {
                throw new KeyNotFoundException("You are not following this user.");
            }

            _context.Follows.Remove(follow);

            // Update user follower/following counts
            var follower = await _context.Users.FindAsync(followerId);
            var following = await _context.Users.FindAsync(followingId);

            if (follower != null && follower.FollowingCount > 0)
            {
                follower.FollowingCount--;
            }

            if (following != null && following.FollowersCount > 0)
            {
                following.FollowersCount--;
            }

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
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            // If we're tracking the count in the user entity, use that
            if (user.FollowersCount.HasValue)
            {
                return user.FollowersCount.Value;
            }

            // Otherwise count from the follows table
            return await _context.Follows.CountAsync(f => f.FollowingId == userId);
        }

        public async Task<int> GetFollowingCountAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            // If we're tracking the count in the user entity, use that
            if (user.FollowingCount.HasValue)
            {
                return user.FollowingCount.Value;
            }

            // Otherwise count from the follows table
            return await _context.Follows.CountAsync(f => f.FollowerId == userId);
        }
    }
}
