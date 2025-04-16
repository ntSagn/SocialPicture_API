using SocialPicture.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialPicture.Application.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public int? FollowersCount { get; set; }
        public int? FollowingCount { get; set; }
        public int? PostsCount { get; set; }

        public UserRole Role { get; set; }
        public string? ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateUserDto
    {
        public string? Fullname { get; set; }
        public string? Email { get; set; }
        public string? Bio { get; set; }

        public string? ProfilePicture { get; set; }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ChangeUserRoleDto
    {
        public UserRole NewRole { get; set; }
    }
}
