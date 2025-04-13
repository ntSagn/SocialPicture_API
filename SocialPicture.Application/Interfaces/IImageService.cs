using Microsoft.AspNetCore.Http;
using SocialPicture.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialPicture.Application.Interfaces
{
    public interface IImageService
    {
        Task<IEnumerable<ImageDto>> GetAllImagesAsync(int? userId = null, bool publicOnly = true, int? currentUserId = null);
        Task<ImageDto> GetImageByIdAsync(int id, int? currentUserId = null);
        Task<ImageDto> CreateImageAsync(int userId, CreateImageDto createImageDto, IFormFile imageFile);
        Task<ImageDto> UpdateImageAsync(int id, int userId, UpdateImageDto updateImageDto);
        Task<bool> DeleteImageAsync(int id, int userId);
    }


}
