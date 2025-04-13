using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialPicture.Domain.Entities
{
    public class Follow
    {
        public int FollowId { get; set; }
        public int FollowerId { get; set; }
        public int FollowingId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User Follower { get; set; } = null!;
        public User Following { get; set; } = null!;
    }
}
