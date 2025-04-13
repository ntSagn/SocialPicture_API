using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialPicture.Application.DTOs
{
    public class ImageDto
    {
        public int ImageId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public bool IsSavedByCurrentUser { get; set; }
    }

    public class CreateImageDto
    {
        public string Caption { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = true;
        // The actual image file will be handled separately
    }

    public class UpdateImageDto
    {
        public string? Caption { get; set; }
        public bool? IsPublic { get; set; }
    }
}
