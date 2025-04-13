using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using System.Security.Claims;

namespace SocialPicture.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;
        private readonly ILikeService _likeService;

        public ImagesController(IImageService imageService, ILikeService likeService)
        {
            _imageService = imageService;
            _likeService = likeService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ImageDto>>> GetAllImages([FromQuery] int? userId = null, [FromQuery] bool publicOnly = true)
        {
            int? currentUserId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            }

            var images = await _imageService.GetAllImagesAsync(userId, publicOnly, currentUserId);
            return Ok(images);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<ImageDto>> GetImageById(int id)
        {
            try
            {
                var image = await _imageService.GetImageByIdAsync(id);
                
                // If authenticated, check if user liked the image
                if (User.Identity?.IsAuthenticated == true)
                {
                    var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    image.IsLikedByCurrentUser = await _likeService.HasUserLikedImageAsync(currentUserId, image.ImageId);
                }
                
                return Ok(image);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ImageDto>> CreateImage([FromForm] CreateImageDto createImageDto, IFormFile imageFile)
        {
            if (imageFile == null)
            {
                return BadRequest("Image file is required.");
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var image = await _imageService.CreateImageAsync(userId, createImageDto, imageFile);
                return CreatedAtAction(nameof(GetImageById), new { id = image.ImageId }, image);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<ImageDto>> UpdateImage(int id, UpdateImageDto updateImageDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var image = await _imageService.UpdateImageAsync(id, userId, updateImageDto);
                return Ok(image);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImage(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _imageService.DeleteImageAsync(id, userId);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }
    }
}
