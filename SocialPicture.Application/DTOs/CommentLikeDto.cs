namespace SocialPicture.Application.DTOs
{
    public class CommentLikeDto
    {
        public int CommentLikeId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int CommentId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
