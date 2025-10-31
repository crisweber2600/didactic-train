using Microsoft.AspNetCore.Mvc;
using SharePointDeduplicator.API.Models;
using SharePointDeduplicator.API.Services;

namespace SharePointDeduplicator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SharePointController : ControllerBase
{
    private readonly ISharePointScannerService _scannerService;
    private readonly ILogger<SharePointController> _logger;

    public SharePointController(ISharePointScannerService scannerService, ILogger<SharePointController> logger)
    {
        _scannerService = scannerService;
        _logger = logger;
    }

    [HttpPost("scan")]
    public async Task<ActionResult<ScanReport>> ScanSite([FromBody] ScanRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SiteUrl))
            {
                return BadRequest("Site URL is required");
            }

            _logger.LogInformation("Scanning SharePoint site: {SiteUrl}", request.SiteUrl);
            var report = await _scannerService.ScanSiteAsync(request.SiteUrl, cancellationToken);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning site");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("scan/{scanId}")]
    public ActionResult<ScanReport> GetScanReport(string scanId)
    {
        try
        {
            var report = _scannerService.GetScanReport(scanId);
            if (report == null)
            {
                return NotFound($"Scan report not found: {scanId}");
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scan report");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("replace")]
    public async Task<ActionResult<ReplacementResult>> ReplaceWithShortcuts([FromBody] ReplacementRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ScanId))
            {
                return BadRequest("Scan ID is required");
            }

            if (request.Selections == null || request.Selections.Count == 0)
            {
                return BadRequest("At least one replacement selection is required");
            }

            _logger.LogInformation("Replacing duplicates with shortcuts for scan: {ScanId}", request.ScanId);
            var result = await _scannerService.ReplaceWithShortcutsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing files with shortcuts");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("verify/{scanId}")]
    public async Task<ActionResult<VerificationResult>> VerifyShortcuts(string scanId, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(scanId))
            {
                return BadRequest("Scan ID is required");
            }

            _logger.LogInformation("Verifying shortcuts for scan: {ScanId}", scanId);
            var result = await _scannerService.VerifyShortcutsAsync(scanId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying shortcuts");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class ScanRequest
{
    public string SiteUrl { get; set; } = string.Empty;
}
