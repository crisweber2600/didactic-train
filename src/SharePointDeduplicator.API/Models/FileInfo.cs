namespace SharePointDeduplicator.API.Models;

public class FileInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Hash { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string WebUrl { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string DriveId { get; set; } = string.Empty;
}
