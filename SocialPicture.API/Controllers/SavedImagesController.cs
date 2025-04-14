using Braintree;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using System.Security.Claims;

namespace SocialPicture.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SavedImagesController : ControllerBase
    {
        private readonly ISavedImageService _savedImageService;
        private readonly IImageService _imageService;

        public SavedImagesController(ISavedImageService savedImageService, IImageService imageService)
        {
            _savedImageService = savedImageService;
            _imageService = imageService;
        }

        /// <summary>
        /// Gets all images saved by the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ImageDto>>> GetMySavedImages()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var savedImages = await _savedImageService.GetSavedImagesByUserIdAsync(userId);
                return Ok(savedImages);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Gets all images saved by a specific user (requires authorization)
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ImageDto>>> GetUserSavedImages(int userId)
        {
            try
            {
                // Only allow admins or the user themselves to see saved images
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (currentUserId != userId && userRole != "ADMIN")
                {
                    return Forbid("You do not have permission to view this user's saved images.");
                }

                var savedImages = await _savedImageService.GetSavedImagesByUserIdAsync(userId);
                return Ok(savedImages);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Saves an image for the current user
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SavedImageDto>> SaveImage(SaveImageDto saveImageDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var savedImage = await _savedImageService.SaveImageAsync(userId, saveImageDto.ImageId);
                return CreatedAtAction(nameof(GetMySavedImages), savedImage);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        /// <summary>
        /// Unsaves an image for the current user
        /// </summary>
        [HttpDelete("{imageId}")]
        public async Task<IActionResult> UnsaveImage(int imageId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _savedImageService.UnsaveImageAsync(userId, imageId);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Checks if the current user has saved a specific image
        /// </summary>
        [HttpGet("check/{imageId}")]
        public async Task<ActionResult<bool>> HasUserSavedImage(int imageId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _savedImageService.HasUserSavedImageAsync(userId, imageId);
            return Ok(new { isSaved = result });
        }

        /// <summary>
        /// Gets paginated saved images by the current user
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PaginatedResult<ImageDto>>> GetPagedSavedImages(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                // Get all saved images first (this should be refactored to support pagination at database level)
                var allSavedImages = (await _savedImageService.GetSavedImagesByUserIdAsync(userId)).ToList();
                
                // Calculate pagination
                var totalCount = allSavedImages.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                
                // Validate page number
                if (page < 1) page = 1;
                if (page > totalPages && totalPages > 0) page = totalPages;
                
                // Get paginated subset
                var pagedImages = allSavedImages
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                var result = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages,
                    items = pagedImages
                };
                
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
