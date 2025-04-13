using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialPicture.Domain.Entities
{
    public class CommentLike
    {
        public int CommentLikeId { get; set; }
        public int UserId { get; set; }
        public int CommentId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public Comment Comment { get; set; } = null!;
    }
}
