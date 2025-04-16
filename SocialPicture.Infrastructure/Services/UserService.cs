using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using SocialPicture.Domain.Entities;
using SocialPicture.Persistence;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocialPicture.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users.ToListAsync();
            return users.Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                Fullname = u.Fullname,
                Bio = u.Bio,
                Role = u.Role,
                ProfilePicture = u.ProfilePicture,
                CreatedAt = u.CreatedAt
            });
        }

        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {id} not found.");
            }

            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Fullname = user.Fullname,
                Bio = user.Bio,
                Role = user.Role,
                ProfilePicture = user.ProfilePicture,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<UserDto> GetUserByUsernameAsync(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with username {username} not found.");
            }

            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Fullname = user.Fullname,
                Bio = user.Bio,
                Role = user.Role,
                ProfilePicture = user.ProfilePicture,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<UserDto> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {id} not found.");
            }

            if (!string.IsNullOrWhiteSpace(updateUserDto.Fullname))
            {
                user.Fullname = updateUserDto.Fullname;
            }

            if (!string.IsNullOrWhiteSpace(updateUserDto.Email))
            {
                // Check if email is already in use by another user
                var emailExists = await _context.Users.AnyAsync(u => u.Email == updateUserDto.Email && u.UserId != id);
                if (emailExists)
                {
                    throw new InvalidOperationException("Email is already in use by another user.");
                }
                user.Email = updateUserDto.Email;
            }

            // Add this block to handle Bio updates
            if (updateUserDto.Bio != null)
            {
                user.Bio = updateUserDto.Bio;
            }

            if (updateUserDto.ProfilePicture != null)
            {
                user.ProfilePicture = updateUserDto.ProfilePicture;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Fullname = user.Fullname,
                Bio = user.Bio,
                Role = user.Role,
                ProfilePicture = user.ProfilePicture,
                CreatedAt = user.CreatedAt,
                FollowersCount = user.FollowersCount,
                FollowingCount = user.FollowingCount,
                PostsCount = user.PostsCount
            };
        }


        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            // Validate the DTO
            if (string.IsNullOrEmpty(changePasswordDto.CurrentPassword) ||
                string.IsNullOrEmpty(changePasswordDto.NewPassword))
            {
                throw new ArgumentException("Current password and new password are required");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            // Validate current password
            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, changePasswordDto.CurrentPassword);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedAccessException("Current password is incorrect.");
            }

            // Hash new password
            user.Password = _passwordHasher.HashPassword(user, changePasswordDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }


        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {id} not found.");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<UserDto> ChangeUserRoleAsync(int userId, ChangeUserRoleDto changeUserRoleDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            // Set the new role
            user.Role = changeUserRoleDto.NewRole;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Fullname = user.Fullname,
                Bio = user.Bio,
                Role = user.Role,
                ProfilePicture = user.ProfilePicture,
                CreatedAt = user.CreatedAt,
                FollowersCount = user.FollowersCount,
                FollowingCount = user.FollowingCount,
                PostsCount = user.PostsCount
            };
        }
    }
}
