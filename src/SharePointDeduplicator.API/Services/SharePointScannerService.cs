using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using SharePointDeduplicator.API.Models;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace SharePointDeduplicator.API.Services;

public interface ISharePointScannerService
{
    Task<ScanReport> ScanSiteAsync(string siteUrl, CancellationToken cancellationToken = default);
    Task<ReplacementResult> ReplaceWithShortcutsAsync(ReplacementRequest request, CancellationToken cancellationToken = default);
    Task<VerificationResult> VerifyShortcutsAsync(string scanId, CancellationToken cancellationToken = default);
    ScanReport? GetScanReport(string scanId);
}

public class SharePointScannerService : ISharePointScannerService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<SharePointScannerService> _logger;
    private readonly ScannerOptions _options;
    private readonly ConcurrentDictionary<string, ScanReport> _scanReports = new();
    private readonly ConcurrentDictionary<string, List<string>> _shortcutMappings = new();

    public SharePointScannerService(
        GraphServiceClient graphClient, 
        ILogger<SharePointScannerService> logger,
        IOptions<ScannerOptions> options)
    {
        _graphClient = graphClient;
        _logger = logger;
        _options = options.Value;
    }

    public ScanReport? GetScanReport(string scanId)
    {
        _scanReports.TryGetValue(scanId, out var report);
        return report;
    }

    public async Task<ScanReport> ScanSiteAsync(string siteUrl, CancellationToken cancellationToken = default)
    {
        var report = new ScanReport { SiteUrl = siteUrl };
        _scanReports[report.ScanId] = report;

        try
        {
            _logger.LogInformation("Starting scan of SharePoint site: {SiteUrl}", siteUrl);

            // Parse site URL to get site ID
            var site = await GetSiteFromUrlAsync(siteUrl, cancellationToken);
            if (site?.Id == null)
            {
                throw new Exception("Unable to retrieve site information");
            }

            // Get all drives (document libraries) in the site
            var drives = await _graphClient.Sites[site.Id].Drives.GetAsync(cancellationToken: cancellationToken);
            if (drives?.Value == null)
            {
                throw new Exception("No drives found in the site");
            }

            var allFiles = new List<Models.FileInfo>();

            // Scan each drive
            foreach (var drive in drives.Value.Where(d => d.Id != null))
            {
                _logger.LogInformation("Scanning drive: {DriveName}", drive.Name);
                var files = await ScanDriveAsync(site.Id, drive.Id!, cancellationToken);
                allFiles.AddRange(files);
            }

            report.TotalFilesScanned = allFiles.Count;

            // Group files by hash to find duplicates
            var duplicateGroups = allFiles
                .Where(f => !string.IsNullOrEmpty(f.Hash))
                .GroupBy(f => f.Hash)
                .Where(g => g.Count() > 1)
                .Select(g => new DuplicateGroup
                {
                    Hash = g.Key,
                    FileSize = g.First().Size,
                    Files = g.ToList()
                })
                .ToList();

            report.DuplicateGroups = duplicateGroups;
            report.DuplicateFilesFound = duplicateGroups.Sum(g => g.Files.Count);
            report.TotalSpaceWasted = duplicateGroups.Sum(g => g.TotalWastedSpace);
            report.Status = ScanStatus.Completed;

            _logger.LogInformation("Scan completed. Found {Count} duplicate groups", duplicateGroups.Count);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Scan was cancelled for site {SiteUrl}", siteUrl);
            report.Status = ScanStatus.Failed;
            report.ErrorMessage = "Scan was cancelled.";
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
        {
            _logger.LogError(ex, "Graph API error scanning SharePoint site");
            report.Status = ScanStatus.Failed;
            report.ErrorMessage = ex.Message;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            _logger.LogError(ex, "Unexpected error scanning SharePoint site");
            report.Status = ScanStatus.Failed;
            report.ErrorMessage = ex.Message;
        }

        return report;
    }

    private async Task<Site?> GetSiteFromUrlAsync(string siteUrl, CancellationToken cancellationToken)
    {
        try
        {
            // Extract host and site path from URL
            var uri = new Uri(siteUrl);
            var hostName = uri.Host;
            var sitePath = uri.AbsolutePath;

            // Use Graph API to get site by URL
            var site = await _graphClient.Sites[$"{hostName}:{sitePath}"].GetAsync(cancellationToken: cancellationToken);
            return site;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting site from URL: {SiteUrl}", siteUrl);
            throw;
        }
    }

    private async Task<List<Models.FileInfo>> ScanDriveAsync(string siteId, string driveId, CancellationToken cancellationToken)
    {
        var files = new List<Models.FileInfo>();

        try
        {
            await ScanDriveItemsAsync(siteId, driveId, "root", files, cancellationToken);
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
        {
            _logger.LogError(ex, "Graph API error scanning drive {DriveId}", driveId);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            _logger.LogError(ex, "Unexpected error scanning drive {DriveId}", driveId);
        }

        return files;
    }

    private async Task ScanDriveItemsAsync(string siteId, string driveId, string itemId, List<Models.FileInfo> files, CancellationToken cancellationToken)
    {
        try
        {
            // In MS Graph SDK v5+, we need to handle pagination to get all items
            // Microsoft Graph paginates children collections (typically 200 items per page)
            var requestBuilder = _graphClient.Drives[driveId].Items[itemId].Children;
            var childrenResponse = await requestBuilder.GetAsync(cancellationToken: cancellationToken);
            
            // Track page count for safety limit (from configuration)
            int pageCount = 0;
            int maxPages = _options.MaxPagesPerDirectory;
            
            // Process all pages of results
            while (childrenResponse != null && pageCount < maxPages)
            {
                if (childrenResponse.Value == null) break;

                // Only increment page count for pages that contain actual items
                if (childrenResponse.Value.Count > 0)
                {
                    pageCount++;
                }

                foreach (var item in childrenResponse.Value)
                {
                    if (item.Folder != null)
                    {
                        // Recursively scan folders
                        if (item.Id != null)
                        {
                            await ScanDriveItemsAsync(siteId, driveId, item.Id, files, cancellationToken);
                        }
                    }
                    else if (item.File != null && item.Id != null && item.Size.HasValue)
                    {
                        // Process file
                        var fileInfo = new Models.FileInfo
                        {
                            Id = item.Id,
                            Name = item.Name ?? "Unknown",
                            Path = GetItemPath(item),
                            Size = item.Size.Value,
                            Hash = item.File.Hashes?.QuickXorHash ?? item.File.Hashes?.Sha1Hash ?? string.Empty,
                            LastModified = item.LastModifiedDateTime?.DateTime ?? DateTime.MinValue,
                            WebUrl = item.WebUrl ?? string.Empty,
                            SiteId = siteId,
                            DriveId = driveId
                        };

                        files.Add(fileInfo);
                    }
                }

                // Check if there's a next page and fetch it using the @odata.nextLink
                if (!string.IsNullOrEmpty(childrenResponse.OdataNextLink))
                {
                    // Create a new request with the next page URL
                    var nextPageRequestInfo = new RequestInformation
                    {
                        HttpMethod = Method.GET,
                        URI = new Uri(childrenResponse.OdataNextLink)
                    };
                    
                    childrenResponse = await _graphClient.RequestAdapter.SendAsync(
                        nextPageRequestInfo,
                        DriveItemCollectionResponse.CreateFromDiscriminatorValue,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    // No more pages
                    break;
                }
            }
            
            if (pageCount >= maxPages)
            {
                _logger.LogWarning("Reached maximum page limit ({MaxPages}) scanning drive {DriveId}, item {ItemId}", maxPages, driveId, itemId);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            _logger.LogError(ex, "Error scanning items in drive {DriveId}, item {ItemId}", driveId, itemId);
        }
    }

    private string GetItemPath(DriveItem item)
    {
        if (item.ParentReference?.Path != null)
        {
            var path = item.ParentReference.Path;
            // Remove the "/drive/root:" prefix if present
            if (path.Contains("/drive/root:"))
            {
                path = path.Substring(path.IndexOf("/drive/root:") + "/drive/root:".Length);
            }
            return $"{path}/{item.Name}";
        }
        return item.Name ?? "Unknown";
    }

    public async Task<ReplacementResult> ReplaceWithShortcutsAsync(ReplacementRequest request, CancellationToken cancellationToken = default)
    {
        var result = new ReplacementResult { ScanId = request.ScanId };
        
        if (!_scanReports.TryGetValue(request.ScanId, out var scanReport))
        {
            throw new Exception($"Scan report not found: {request.ScanId}");
        }

        var shortcutPaths = new List<string>();

        try
        {
            foreach (var selection in request.Selections)
            {
                var group = scanReport.DuplicateGroups.FirstOrDefault(g => g.Hash == selection.Hash);
                if (group == null) continue;

                var trueCopy = group.Files.FirstOrDefault(f => f.Id == selection.TrueCopyFileId);
                if (trueCopy == null) continue;

                group.SelectedTrueCopy = trueCopy;

                // Replace each duplicate with a shortcut
                foreach (var duplicate in group.Files.Where(f => f.Id != selection.TrueCopyFileId))
                {
                    var detail = new ReplacementDetail
                    {
                        OriginalFileId = duplicate.Id,
                        OriginalPath = duplicate.Path
                    };

                    try
                    {
                        // Create a .url shortcut file
                        var shortcutContent = CreateShortcutContent(trueCopy.WebUrl);
                        var shortcutName = $"{Path.GetFileNameWithoutExtension(duplicate.Name)}.url";
                        
                        // Upload the shortcut file to the same location
                        await UploadShortcutFileAsync(duplicate.SiteId, duplicate.DriveId, duplicate.Path, shortcutName, shortcutContent, cancellationToken);
                        
                        // Delete the original duplicate file
                        await _graphClient.Drives[duplicate.DriveId].Items[duplicate.Id]
                            .DeleteAsync(cancellationToken: cancellationToken);

                        detail.ShortcutPath = $"{Path.GetDirectoryName(duplicate.Path)}/{shortcutName}";
                        detail.Success = true;
                        result.SuccessfulReplacements++;

                        shortcutPaths.Add(detail.ShortcutPath);

                        _logger.LogInformation("Replaced file {Path} with shortcut", duplicate.Path);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogError(ex, "Unauthorized access replacing file {Path}", duplicate.Path);
                        detail.Success = false;
                        detail.ErrorMessage = ex.Message;
                        result.FailedReplacements++;
                    }
                    catch (System.IO.IOException ex)
                    {
                        _logger.LogError(ex, "IO error replacing file {Path}", duplicate.Path);
                        detail.Success = false;
                        detail.ErrorMessage = ex.Message;
                        result.FailedReplacements++;
                    }
                    catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
                    {
                        _logger.LogError(ex, "Graph API error replacing file {Path}", duplicate.Path);
                        detail.Success = false;
                        detail.ErrorMessage = ex.Message;
                        result.FailedReplacements++;
                    }
                    catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                    {
                        _logger.LogError(ex, "Unexpected error replacing file {Path}", duplicate.Path);
                        detail.Success = false;
                        detail.ErrorMessage = ex.Message;
                        result.FailedReplacements++;
                    }

                    result.Details.Add(detail);
                    result.TotalReplacements++;
                }
            }

            // Store shortcut paths for verification
            _shortcutMappings[request.ScanId] = shortcutPaths;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during replacement process");
            throw;
        }

        return result;
    }

    private string CreateShortcutContent(string targetUrl)
    {
        // Create a Windows .url shortcut file content
        return $"[InternetShortcut]\r\nURL={targetUrl}\r\n";
    }

    private async Task UploadShortcutFileAsync(string siteId, string driveId, string originalPath, string shortcutName, string content, CancellationToken cancellationToken)
    {
        try
        {
            // Get the parent folder path
            var parentPath = Path.GetDirectoryName(originalPath)?.Replace("\\", "/") ?? "";
            if (parentPath.StartsWith("/"))
            {
                parentPath = parentPath.Substring(1);
            }

            // Upload the shortcut file
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            
            var uploadPath = string.IsNullOrEmpty(parentPath) 
                ? $"root:/{shortcutName}:" 
                : $"root:/{parentPath}/{shortcutName}:";

            await _graphClient.Drives[driveId].Items[uploadPath].Content
                .PutAsync(stream, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading shortcut file {ShortcutName}", shortcutName);
            throw;
        }
    }

    public async Task<VerificationResult> VerifyShortcutsAsync(string scanId, CancellationToken cancellationToken = default)
    {
        var result = new VerificationResult { ScanId = scanId };

        if (!_shortcutMappings.TryGetValue(scanId, out var shortcutPaths))
        {
            throw new Exception($"No shortcuts found for scan: {scanId}");
        }

        if (!_scanReports.TryGetValue(scanId, out var scanReport))
        {
            throw new Exception($"Scan report not found: {scanId}");
        }

        result.TotalShortcutsChecked = shortcutPaths.Count;

        foreach (var shortcutPath in shortcutPaths)
        {
            try
            {
                // Find the drive and site for this shortcut
                var firstGroup = scanReport.DuplicateGroups.FirstOrDefault(g => g.SelectedTrueCopy != null);
                if (firstGroup?.SelectedTrueCopy == null) continue;

                var driveId = firstGroup.SelectedTrueCopy.DriveId;

                // Try to retrieve the shortcut file
                var shortcutFileName = Path.GetFileName(shortcutPath);
                var parentPath = Path.GetDirectoryName(shortcutPath)?.Replace("\\", "/") ?? "";
                if (parentPath.StartsWith("/"))
                {
                    parentPath = parentPath.Substring(1);
                }

                var itemPath = string.IsNullOrEmpty(parentPath)
                    ? $"root:/{shortcutFileName}"
                    : $"root:/{parentPath}/{shortcutFileName}";

                var item = await _graphClient.Drives[driveId].Items[itemPath]
                    .GetAsync(cancellationToken: cancellationToken);

                if (item != null)
                {
                    result.ValidShortcuts++;
                }
                else
                {
                    result.BrokenShortcuts++;
                    result.BrokenDetails.Add(new BrokenShortcutDetail
                    {
                        ShortcutPath = shortcutPath,
                        TargetPath = string.Empty,
                        Issue = "Shortcut file not found"
                    });
                }
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
            {
                _logger.LogError(ex, "Graph API error verifying shortcut {Path}", shortcutPath);
                result.BrokenShortcuts++;
                result.BrokenDetails.Add(new BrokenShortcutDetail
                {
                    ShortcutPath = shortcutPath,
                    TargetPath = string.Empty,
                    Issue = $"Graph API error: {ex.Message}"
                });
            }
            catch (System.IO.IOException ex)
            {
                _logger.LogError(ex, "IO error verifying shortcut {Path}", shortcutPath);
                result.BrokenShortcuts++;
                result.BrokenDetails.Add(new BrokenShortcutDetail
                {
                    ShortcutPath = shortcutPath,
                    TargetPath = string.Empty,
                    Issue = $"IO error: {ex.Message}"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access verifying shortcut {Path}", shortcutPath);
                result.BrokenShortcuts++;
                result.BrokenDetails.Add(new BrokenShortcutDetail
                {
                    ShortcutPath = shortcutPath,
                    TargetPath = string.Empty,
                    Issue = $"Unauthorized access: {ex.Message}"
                });
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                _logger.LogError(ex, "Unexpected error verifying shortcut {Path}", shortcutPath);
                result.BrokenShortcuts++;
                result.BrokenDetails.Add(new BrokenShortcutDetail
                {
                    ShortcutPath = shortcutPath,
                    TargetPath = string.Empty,
                    Issue = ex.Message
                });
            }
        }

        return result;
    }
}
