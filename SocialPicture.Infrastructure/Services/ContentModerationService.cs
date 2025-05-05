using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SocialPicture.Application.Interfaces;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;

namespace SocialPicture.Infrastructure.Services
{
    public class ContentModerationService : IContentModerationService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _apiUser;
        private readonly string _apiSecret;

        public ContentModerationService(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _apiUser = configuration["SightEngine:ApiUser"];
            _apiSecret = configuration["SightEngine:ApiSecret"];
        }

        public async Task<(bool isAppropriate, string message)> CheckImageContentAsync(string imageUrl)
        {
            // This method is kept for backward compatibility but won't be used
            throw new NotImplementedException("URL-based checking is not supported. Use file-based checking instead.");
        }

        public async Task<(bool isAppropriate, string message)> CheckImageFileContentAsync(IFormFile imageFile)
        {
            try
            {
                // Create HttpClient
                var client = _clientFactory.CreateClient();

                // Create multipart form content
                using var content = new MultipartFormDataContent();

                // Add the API credentials
                content.Add(new StringContent(_apiUser), "api_user");
                content.Add(new StringContent(_apiSecret), "api_secret");
                content.Add(new StringContent("nudity-2.1,gore-2.0"), "models");

                // Add the image file
                using var fileStream = imageFile.OpenReadStream();
                using var streamContent = new StreamContent(fileStream);
                content.Add(streamContent, "media", imageFile.FileName);

                // Make the API request
                var response = await client.PostAsync("https://api.sightengine.com/1.0/check.json", content);
                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<SightEngineResponse>(responseContent);

                // Analyze the response
                if (result.Status == "success")
                {
                    // Check nudity
                    if (result.Nudity.Sexual_activity > 0.4 ||
                        result.Nudity.Sexual_display > 0.4 ||
                        result.Nudity.Erotica > 0.5)
                    {
                        return (false, "The image contains inappropriate adult content that violates our community standards.");
                    }

                    // Check gore
                    if (result.Gore.Prob > 0.4)
                    {
                        return (false, "The image contains violent or graphic content that violates our community standards.");
                    }

                    return (true, "Image passed content moderation.");
                }

                return (false, "Failed to analyze image content.");
            }
            catch (Exception ex)
            {
                return (false, $"Error analyzing image content: {ex.Message}");
            }
        }
    }

    // Classes to deserialize API response remain the same
    public class SightEngineResponse
    {
        public string Status { get; set; }
        public NudityResult Nudity { get; set; }
        public GoreResult Gore { get; set; }
    }

    public class NudityResult
    {
        public double Sexual_activity { get; set; }
        public double Sexual_display { get; set; }
        public double Erotica { get; set; }
        public double None { get; set; }
    }

    public class GoreResult
    {
        public double Prob { get; set; }
    }
}