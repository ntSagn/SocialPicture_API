using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SocialPicture.Application.Interfaces
{
    public interface IContentModerationService
    {
        Task<(bool isAppropriate, string message)> CheckImageContentAsync(string imageUrl);
        Task<(bool isAppropriate, string message)> CheckImageFileContentAsync(IFormFile imageFile);
    }
}