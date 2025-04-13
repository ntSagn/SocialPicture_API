using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using System.Security.Claims;

namespace SocialPicture.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagsController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TagDto>>> GetAllTags()
        {
            var tags = await _tagService.GetAllTagsAsync();
            return Ok(tags);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TagDto>> GetTagById(int id)
        {
            try
            {
                var tag = await _tagService.GetTagByIdAsync(id);
                return Ok(tag);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("name/{name}")]
        public async Task<ActionResult<TagDto>> GetTagByName(string name)
        {
            try
            {
                var tag = await _tagService.GetTagByNameAsync(name);
                return Ok(tag);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<TagDto>> CreateTag(CreateTagDto createTagDto)
        {
            try
            {
                var tag = await _tagService.CreateTagAsync(createTagDto);
                return CreatedAtAction(nameof(GetTagById), new { id = tag.TagId }, tag);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            try
            {
                var result = await _tagService.DeleteTagAsync(id);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("images/tag/{tagId}")]
        public async Task<ActionResult<IEnumerable<ImageDto>>> GetImagesByTagId(int tagId)
        {
            try
            {
                int? currentUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                }
                
                var images = await _tagService.GetImagesByTagIdAsync(tagId, currentUserId);
                return Ok(images);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("images/name/{tagName}")]
        public async Task<ActionResult<IEnumerable<ImageDto>>> GetImagesByTagName(string tagName)
        {
            try
            {
                int? currentUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                }
                
                var images = await _tagService.GetImagesByTagNameAsync(tagName, currentUserId);
                return Ok(images);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("image/{imageId}/tag/{tagId}")]
        public async Task<IActionResult> AddTagToImage(int imageId, int tagId)
        {
            try
            {
                var result = await _tagService.AddTagToImageAsync(imageId, tagId);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("image/{imageId}/tag/{tagId}")]
        public async Task<IActionResult> RemoveTagFromImage(int imageId, int tagId)
        {
            try
            {
                var result = await _tagService.RemoveTagFromImageAsync(imageId, tagId);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("image/{imageId}")]
        public async Task<ActionResult<IEnumerable<TagDto>>> GetTagsByImageId(int imageId)
        {
            try
            {
                var tags = await _tagService.GetTagsByImageIdAsync(imageId);
                return Ok(tags);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("popular")]
        public async Task<ActionResult<IEnumerable<TagDto>>> GetPopularTags([FromQuery] int count = 10)
        {
            var tags = await _tagService.GetPopularTagsAsync(count);
            return Ok(tags);
        }
    }
}

