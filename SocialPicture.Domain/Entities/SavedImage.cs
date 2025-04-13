using System;
using System.Collections.Generic;

namespace SocialPicture.Domain.Entities
{
    public class SavedImage
    {
        public int SavedImageId { get; set; }
        public int UserId { get; set; }
        public int ImageId { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public User User { get; set; } = null!;
        public Image Image { get; set; } = null!;
    }
}
