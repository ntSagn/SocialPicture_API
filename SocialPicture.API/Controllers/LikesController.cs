using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SocialPicture.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LikesController : ControllerBase
    {
        private readonly ILikeService _likeService;

        public LikesController(ILikeService likeService)
        {
            _likeService = likeService;
        }

        [HttpGet("image/{imageId}")]
        public async Task<ActionResult<IEnumerable<LikeDto>>> GetLikesByImageId(int imageId)
        {
            try
            {
                var likes = await _likeService.GetLikesByImageIdAsync(imageId);
                return Ok(likes);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Get all images liked by a specific user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ImageDto>>> GetLikedImagesByUserId(int userId)
        {
            try
            {
                int? currentUserId = null;
                // Check if there's a logged-in user
                if (User.Identity.IsAuthenticated)
                {
                    currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                }

                var images = await _likeService.GetLikedImagesByUserIdAsync(userId, currentUserId);
                return Ok(images);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Get all images liked by the current authenticated user
        /// </summary>
        [Authorize]
        [HttpGet("my-likes")]
        public async Task<ActionResult<IEnumerable<ImageDto>>> GetMyLikedImages()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var images = await _likeService.GetLikedImagesByUserIdAsync(userId, userId);
                return Ok(images);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpPost("image/{imageId}")]
        public async Task<ActionResult<LikeDto>> LikeImage(int imageId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var like = await _likeService.LikeImageAsync(userId, imageId);
                return CreatedAtAction(nameof(GetLikesByImageId), new { imageId = like.ImageId }, like);
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
        [HttpDelete("image/{imageId}")]
        public async Task<IActionResult> UnlikeImage(int imageId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _likeService.UnlikeImageAsync(userId, imageId);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("check/{imageId}")]
        public async Task<ActionResult<bool>> CheckIfUserLikedImage(int imageId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var hasLiked = await _likeService.HasUserLikedImageAsync(userId, imageId);
            return Ok(hasLiked);
        }
    }
}
