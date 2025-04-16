using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialPicture.Domain.Enums
{
    public enum NotificationType
    {
        Like,           // Someone liked your image
        CommentLike,    // Someone liked your comment
        Comment,        // Someone commented on your image
        Follow,         // Someone followed you
        ReportResolution,
        ImageDeletion
    }
}
