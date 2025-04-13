using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialPicture.Application.DTOs
{
    public class LikeDto
    {
        public int LikeId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int ImageId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
