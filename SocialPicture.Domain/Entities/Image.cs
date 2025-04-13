using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SocialPicture.Domain.Entities
{
    public class Image
    {
        public int ImageId { get; set; }
        public int UserId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<Like> Likes { get; set; } = new List<Like>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<ImageTag> ImageTags { get; set; } = new List<ImageTag>();
        public ICollection<SavedImage> SavedByUsers { get; set; } = new List<SavedImage>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
