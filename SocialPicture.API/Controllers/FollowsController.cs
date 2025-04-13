using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using System.Security.Claims;

namespace SocialPicture.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FollowsController : ControllerBase
    {
        private readonly IFollowService _followService;

        public FollowsController(IFollowService followService)
        {
            _followService = followService;
        }

        [HttpGet("followers/{userId}")]
        public async Task<ActionResult<IEnumerable<FollowerDto>>> GetFollowersByUserId(int userId)
        {
            try
            {
                int? currentUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                }
                
                var followers = await _followService.GetFollowersByUserIdAsync(userId, currentUserId);
                return Ok(followers);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("following/{userId}")]
        public async Task<ActionResult<IEnumerable<FollowingDto>>> GetFollowingByUserId(int userId)
        {
            try
            {
                var following = await _followService.GetFollowingByUserIdAsync(userId);
                return Ok(following);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("{followingId}")]
        public async Task<ActionResult<FollowDto>> FollowUser(int followingId)
        {
            try
            {
                var followerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var follow = await _followService.FollowUserAsync(followerId, followingId);
                return CreatedAtAction(nameof(CheckIfFollowing), new { followingId }, follow);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("{followingId}")]
        public async Task<IActionResult> UnfollowUser(int followingId)
        {
            try
            {
                var followerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _followService.UnfollowUserAsync(followerId, followingId);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("check/{followingId}")]
        [Authorize]
        public async Task<ActionResult<bool>> CheckIfFollowing(int followingId)
        {
            var followerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var isFollowing = await _followService.IsFollowingAsync(followerId, followingId);
            return Ok(isFollowing);
        }

        [HttpGet("counts/{userId}")]
        public async Task<ActionResult<object>> GetFollowCounts(int userId)
        {
            try
            {
                var followersCount = await _followService.GetFollowersCountAsync(userId);
                var followingCount = await _followService.GetFollowingCountAsync(userId);
                
                return Ok(new { 
                    followersCount, 
                    followingCount 
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}

