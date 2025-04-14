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
    public class CommentLikesController : ControllerBase
    {
        private readonly ICommentLikeService _commentLikeService;

        public CommentLikesController(ICommentLikeService commentLikeService)
        {
            _commentLikeService = commentLikeService;
        }

        /// <summary>
        /// Get users who liked a comment
        /// </summary>
        [HttpGet("comment/{commentId}")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetLikesByCommentId(int commentId)
        {
            try
            {
                var likes = await _commentLikeService.GetLikesByCommentIdAsync(commentId);
                return Ok(likes);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Like a comment
        /// </summary>
        [Authorize]
        [HttpPost("comment/{commentId}")]
        public async Task<ActionResult<CommentLikeDto>> LikeComment(int commentId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var like = await _commentLikeService.LikeCommentAsync(userId, commentId);
                return Ok(like);
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

        /// <summary>
        /// Unlike a comment
        /// </summary>
        [Authorize]
        [HttpDelete("comment/{commentId}")]
        public async Task<ActionResult<bool>> UnlikeComment(int commentId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _commentLikeService.UnlikeCommentAsync(userId, commentId);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Check if user has liked a comment
        /// </summary>
        [Authorize]
        [HttpGet("check/{commentId}")]
        public async Task<ActionResult<bool>> CheckIfUserLikedComment(int commentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var hasLiked = await _commentLikeService.HasUserLikedCommentAsync(userId, commentId);
            return Ok(new { isLiked = hasLiked });
        }

        /// <summary>
        /// Get likes count for a comment
        /// </summary>
        [HttpGet("count/{commentId}")]
        public async Task<ActionResult<int>> GetLikesCount(int commentId)
        {
            try
            {
                var count = await _commentLikeService.GetLikesCountByCommentIdAsync(commentId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
