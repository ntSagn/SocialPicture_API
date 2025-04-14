using SocialPicture.Application.DTOs;

namespace SocialPicture.Application.Interfaces
{
    public interface IReportService
    {
        Task<IEnumerable<ReportDto>> GetAllReportsAsync(bool includingResolved = false);
        Task<ReportDto> GetReportByIdAsync(int id);
        Task<IEnumerable<ReportDto>> GetReportsByImageIdAsync(int imageId);
        Task<IEnumerable<ReportDto>> GetReportsByReporterIdAsync(int reporterId);
        Task<ReportDto> CreateReportAsync(int reporterId, CreateReportDto createReportDto);
        Task<ReportDto> ResolveReportAsync(int reportId, int resolverId, ResolveReportDto resolveReportDto);
        Task<int> GetPendingReportsCountAsync();
    }
}
