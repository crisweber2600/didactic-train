namespace SharePointDeduplicator.API.Models;

public class ScanReport
{
    public string ScanId { get; set; } = Guid.NewGuid().ToString();
    public DateTime ScanDate { get; set; } = DateTime.UtcNow;
    public string SiteUrl { get; set; } = string.Empty;
    public int TotalFilesScanned { get; set; }
    public int DuplicateFilesFound { get; set; }
    public long TotalSpaceWasted { get; set; }
    public List<DuplicateGroup> DuplicateGroups { get; set; } = new();
    public ScanStatus Status { get; set; } = ScanStatus.InProgress;
    public string? ErrorMessage { get; set; }
}

public enum ScanStatus
{
    InProgress,
    Completed,
    Failed
}
