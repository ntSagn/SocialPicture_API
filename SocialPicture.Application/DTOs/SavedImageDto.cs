namespace SocialPicture.Application.DTOs
{
    public class SavedImageDto
    {
        public int SavedImageId { get; set; }
        public int UserId { get; set; }
        public int ImageId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class SaveImageDto
    {
        public int ImageId { get; set; }
    }
}
