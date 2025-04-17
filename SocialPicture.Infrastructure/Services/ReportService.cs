using Microsoft.EntityFrameworkCore;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using SocialPicture.Domain.Entities;
using SocialPicture.Domain.Enums;
using SocialPicture.Persistence;

namespace SocialPicture.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public ReportService(
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<ReportDto>> GetAllReportsAsync(bool includingResolved = false)
        {
            var query = _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.Image)
                .Include(r => r.ResolvedBy)
                .AsQueryable();

            if (!includingResolved)
            {
                query = query.Where(r => r.Status == ReportStatus.Pending);
            }

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reports.Select(r => new ReportDto
            {
                ReportId = r.ReportId,
                ReporterId = r.ReporterId,
                ReporterUsername = r.Reporter.Username,
                ImageId = r.ImageId,
                ImageUrl = r.Image.ImageUrl,
                Reason = r.Reason,
                Status = r.Status,
                ResolvedById = r.ResolvedById,
                ResolvedByUsername = r.ResolvedBy?.Username,
                CreatedAt = r.CreatedAt,
                ResolvedAt = r.ResolvedAt
            });
        }

        public async Task<ReportDto> GetReportByIdAsync(int id)
        {
            var report = await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.Image)
                .Include(r => r.ResolvedBy)
                .FirstOrDefaultAsync(r => r.ReportId == id);

            if (report == null)
            {
                throw new KeyNotFoundException($"Report with ID {id} not found.");
            }

            return new ReportDto
            {
                ReportId = report.ReportId,
                ReporterId = report.ReporterId,
                ReporterUsername = report.Reporter.Username,
                ImageId = report.ImageId,
                ImageUrl = report.Image.ImageUrl,
                Reason = report.Reason,
                Status = report.Status,
                ResolvedById = report.ResolvedById,
                ResolvedByUsername = report.ResolvedBy?.Username,
                CreatedAt = report.CreatedAt,
                ResolvedAt = report.ResolvedAt
            };
        }

        public async Task<IEnumerable<ReportDto>> GetReportsByImageIdAsync(int imageId)
        {
            var reports = await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.Image)
                .Include(r => r.ResolvedBy)
                .Where(r => r.ImageId == imageId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reports.Select(r => new ReportDto
            {
                ReportId = r.ReportId,
                ReporterId = r.ReporterId,
                ReporterUsername = r.Reporter.Username,
                ImageId = r.ImageId,
                ImageUrl = r.Image.ImageUrl,
                Reason = r.Reason,
                Status = r.Status,
                ResolvedById = r.ResolvedById,
                ResolvedByUsername = r.ResolvedBy?.Username,
                CreatedAt = r.CreatedAt,
                ResolvedAt = r.ResolvedAt
            });
        }

        public async Task<IEnumerable<ReportDto>> GetReportsByReporterIdAsync(int reporterId)
        {
            var reports = await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.Image)
                .Include(r => r.ResolvedBy)
                .Where(r => r.ReporterId == reporterId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reports.Select(r => new ReportDto
            {
                ReportId = r.ReportId,
                ReporterId = r.ReporterId,
                ReporterUsername = r.Reporter.Username,
                ImageId = r.ImageId,
                ImageUrl = r.Image.ImageUrl,
                Reason = r.Reason,
                Status = r.Status,
                ResolvedById = r.ResolvedById,
                ResolvedByUsername = r.ResolvedBy?.Username,
                CreatedAt = r.CreatedAt,
                ResolvedAt = r.ResolvedAt
            });
        }

        public async Task<ReportDto> CreateReportAsync(int reporterId, CreateReportDto createReportDto)
        {
            // Check if image exists
            var image = await _context.Images.FindAsync(createReportDto.ImageId);
            if (image == null)
            {
                throw new KeyNotFoundException($"Image with ID {createReportDto.ImageId} not found.");
            }

            // Check if user exists
            var user = await _context.Users.FindAsync(reporterId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {reporterId} not found.");
            }

            // Check if this user has already reported this image
            var existingReport = await _context.Reports
                .FirstOrDefaultAsync(r => r.ReporterId == reporterId && r.ImageId == createReportDto.ImageId && r.Status == ReportStatus.Pending);

            if (existingReport != null)
            {
                throw new InvalidOperationException("You have already reported this image and it's still pending review.");
            }

            // Create report
            var report = new Report
            {
                ReporterId = reporterId,
                ImageId = createReportDto.ImageId,
                Reason = createReportDto.Reason,
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            // Notify moderators (could be done via a separate service)
            // This would be implemented in the notification service

            return new ReportDto
            {
                ReportId = report.ReportId,
                ReporterId = report.ReporterId,
                ReporterUsername = user.Username,
                ImageId = report.ImageId,
                ImageUrl = image.ImageUrl,
                Reason = report.Reason,
                Status = report.Status,
                ResolvedById = null,
                ResolvedByUsername = null,
                CreatedAt = report.CreatedAt,
                ResolvedAt = null
            };
        }

        public async Task<ReportDto> ResolveReportAsync(int reportId, int resolverId, ResolveReportDto resolveReportDto)
        {
            var report = await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.Image)
                .ThenInclude(i => i.User)
                .FirstOrDefaultAsync(r => r.ReportId == reportId);

            if (report == null)
            {
                throw new KeyNotFoundException($"Report with ID {reportId} not found.");
            }

            if (report.Status != ReportStatus.Pending)
            {
                throw new InvalidOperationException("This report has already been resolved.");
            }

            var resolver = await _context.Users.FindAsync(resolverId);
            if (resolver == null)
            {
                throw new KeyNotFoundException($"User (resolver) with ID {resolverId} not found.");
            }

            // Update report status
            report.Status = resolveReportDto.Status;
            report.ResolvedById = resolverId;
            report.ResolvedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Create notification for the reporter
            await _notificationService.CreateNotificationAsync(
                report.ReporterId, 
                NotificationType.ReportResolution,
                report.ReportId,
                $"Your report on an image has been reviewed and marked as {report.Status}.");

            // If the report is upheld and the image needs to be deleted, handle that here
            if (resolveReportDto.Status == ReportStatus.Resolved)
            {
                // Optional: Take action on the image if needed
                // For example, un-publish it or mark it as under review
            }

            return new ReportDto
            {
                ReportId = report.ReportId,
                ReporterId = report.ReporterId,
                ReporterUsername = report.Reporter.Username,
                ImageId = report.ImageId,
                ImageUrl = report.Image.ImageUrl,
                Reason = report.Reason,
                Status = report.Status,
                ResolvedById = report.ResolvedById,
                ResolvedByUsername = resolver.Username,
                CreatedAt = report.CreatedAt,
                ResolvedAt = report.ResolvedAt
            };
        }

        public async Task<int> GetPendingReportsCountAsync()
        {
            return await _context.Reports
                .CountAsync(r => r.Status == ReportStatus.Pending);
        }
    }
}
