using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocialPicture.Domain.Enums;

namespace SocialPicture.Domain.Entities
{
    public class Report
    {
        public int ReportId { get; set; }
        public int ReporterId { get; set; }
        public int ImageId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public ReportStatus Status { get; set; }
        public int? ResolvedById { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Navigation properties
        public User Reporter { get; set; } = null!;
        public Image Image { get; set; } = null!;
        public User? ResolvedBy { get; set; }
    }
}
