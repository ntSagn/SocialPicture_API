using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialPicture.Domain.Entities
{
    public class ImageTag
    {
        public int ImageTagId { get; set; }
        public int ImageId { get; set; }
        public int TagId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Image Image { get; set; } = null!;
        public Tag Tag { get; set; } = null!;
    }
}
