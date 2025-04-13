namespace SocialPicture.Application.DTOs
{
    public class TagDto
    {
        public int TagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ImagesCount { get; set; }
    }

    public class CreateTagDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TagImageDto
    {
        public int ImageId { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}

