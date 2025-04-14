using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialPicture.Application.DTOs;
using SocialPicture.Application.Interfaces;
using System.Security.Claims;

namespace SocialPicture.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Get all reports (admins/managers only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<ActionResult<IEnumerable<ReportDto>>> GetAllReports([FromQuery] bool includingResolved = false)
        {
            var reports = await _reportService.GetAllReportsAsync(includingResolved);
            return Ok(reports);
        }

        /// <summary>
        /// Get report by ID (admins/managers only)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<ActionResult<ReportDto>> GetReportById(int id)
        {
            try
            {
                var report = await _reportService.GetReportByIdAsync(id);
                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Get reports by image ID (admins/managers only)
        /// </summary>
        [HttpGet("image/{imageId}")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<ActionResult<IEnumerable<ReportDto>>> GetReportsByImageId(int imageId)
        {
            var reports = await _reportService.GetReportsByImageIdAsync(imageId);
            return Ok(reports);
        }

        /// <summary>
        /// Get reports made by the current user
        /// </summary>
        [HttpGet("my-reports")]
        public async Task<ActionResult<IEnumerable<ReportDto>>> GetMyReports()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var reports = await _reportService.GetReportsByReporterIdAsync(userId);
            return Ok(reports);
        }

        /// <summary>
        /// Create a report
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ReportDto>> CreateReport(CreateReportDto createReportDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var report = await _reportService.CreateReportAsync(userId, createReportDto);
                return CreatedAtAction(nameof(GetReportById), new { id = report.ReportId }, report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Resolve a report (admins/managers only)
        /// </summary>
        [HttpPut("{id}/resolve")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<ActionResult<ReportDto>> ResolveReport(int id, ResolveReportDto resolveReportDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var report = await _reportService.ResolveReportAsync(id, userId, resolveReportDto);
                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get count of pending reports (admins/managers only)
        /// </summary>
        [HttpGet("pending-count")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<ActionResult<int>> GetPendingReportsCount()
        {
            var count = await _reportService.GetPendingReportsCountAsync();
            return Ok(count);
        }
    }
}
