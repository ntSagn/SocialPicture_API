namespace SocialPicture.Application.DTOs
{
    public class FollowDto
    {
        public int FollowId { get; set; }
        public int FollowerId { get; set; }
        public string FollowerUsername { get; set; } = string.Empty;
        public string? FollowerProfilePicture { get; set; }
        public int FollowingId { get; set; }
        public string FollowingUsername { get; set; } = string.Empty;
        public string? FollowingProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FollowerDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Fullname { get; set; }
        public string ProfilePicture { get; set; }
        public bool IsFollowedByCurrentUser { get; set; }
        public DateTime FollowingSince { get; set; }
    }

    public class FollowingDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Fullname { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime FollowingSince { get; set; }
    }
}

