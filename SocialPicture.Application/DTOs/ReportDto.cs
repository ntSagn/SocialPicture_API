using SocialPicture.Domain.Enums;

namespace SocialPicture.Application.DTOs
{
    public class ReportDto
    {
        public int ReportId { get; set; }
        public int ReporterId { get; set; }
        public string ReporterUsername { get; set; } = string.Empty;
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public ReportStatus Status { get; set; }
        public int? ResolvedById { get; set; }
        public string? ResolvedByUsername { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class CreateReportDto
    {
        public int ImageId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ResolveReportDto
    {
        public ReportStatus Status { get; set; }
        public string? ResolutionComment { get; set; }
    }
}
