using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialPicture.Domain.Entities
{
    public class Like
    {
        public int LikeId { get; set; }
        public int UserId { get; set; }
        public int ImageId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public Image Image { get; set; } = null!;
    }
}
