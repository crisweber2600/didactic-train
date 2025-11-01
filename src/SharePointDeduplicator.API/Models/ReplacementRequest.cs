namespace SharePointDeduplicator.API.Models;

public class ReplacementRequest
{
    public string ScanId { get; set; } = string.Empty;
    public List<ReplacementSelection> Selections { get; set; } = new();
}

public class ReplacementSelection
{
    public string Hash { get; set; } = string.Empty;
    public string TrueCopyFileId { get; set; } = string.Empty;
}
