// SocialPicture.API/Controllers/SearchController.cs
using Microsoft.AspNetCore.Mvc;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using System.Security.Claims;

namespace SocialPicture.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        /// <summary>
        /// Search for images by caption or tags
        /// </summary>
        [HttpGet("images")]
        public async Task<ActionResult<IEnumerable<ImageDto>>> SearchImages(
            [FromQuery] string query, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty");
            }

            int? currentUserId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            }

            var images = await _searchService.SearchImagesAsync(query, currentUserId, page, pageSize);
            return Ok(images);
        }

        /// <summary>
        /// Search for users by username or fullname
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> SearchUsers(
            [FromQuery] string query, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty");
            }

            var users = await _searchService.SearchUsersAsync(query, page, pageSize);
            return Ok(users);
        }

        /// <summary>
        /// Combined search for users and images
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<object>> CombinedSearch(
            [FromQuery] string query, 
            [FromQuery] int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty");
            }

            int? currentUserId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            }

            var usersTask = _searchService.SearchUsersAsync(query, 1, limit);
            var imagesTask = _searchService.SearchImagesAsync(query, currentUserId, 1, limit);

            await Task.WhenAll(usersTask, imagesTask);

            return Ok(new
            {
                users = await usersTask,
                images = await imagesTask
            });
        }
    }
}
