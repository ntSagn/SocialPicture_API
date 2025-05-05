using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SocialPicture.Application.Interfaces;
using SocialPicture.Domain.Entities;
using SocialPicture.Infrastructure.Services;
using SocialPicture.Persistence;
using System.Text;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Register IHttpContextAccessor
        services.AddHttpContextAccessor();
        services.AddHttpClient();
        services.AddScoped<IContentModerationService, ContentModerationService>();
        
        // Add authentication services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<ILikeService, LikeService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IFollowService, FollowService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ISavedImageService, SavedImageService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ICommentLikeService, CommentLikeService>();
        

        // JWT Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")))
            };
        });

        return services;
    }
}
