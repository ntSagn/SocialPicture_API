using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocialPicture.Application.DTOs.Auth;
using SocialPicture.Application.Interfaces;
using SocialPicture.Domain.Entities;
using SocialPicture.Domain.Enums;
using SocialPicture.Persistence;

namespace SocialPicture.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthService(
            ApplicationDbContext context,
            ITokenService tokenService,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthResponseDto> RegisterUserAsync(RegisterDto registerDto)
        {
            // Check if user exists
            if (await _context.Users.AnyAsync(u => 
                u.Username == registerDto.Username || 
                u.Email == registerDto.Email))
            {
                throw new Exception("Username or email already exists");
            }

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                Fullname = registerDto.Fullname,
                Role = UserRole.USER,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user.Password = _passwordHasher.HashPassword(user, registerDto.Password);
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role,
                Token = _tokenService.GenerateToken(user)
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null)
                throw new Exception("Invalid username or password");

            var result = _passwordHasher.VerifyHashedPassword(
                user, user.Password, loginDto.Password);

            if (result == PasswordVerificationResult.Failed)
                throw new Exception("Invalid username or password");

            return new AuthResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role,
                Token = _tokenService.GenerateToken(user)
            };
        }
    }
}
