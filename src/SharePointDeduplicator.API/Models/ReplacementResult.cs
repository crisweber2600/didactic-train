namespace SharePointDeduplicator.API.Models;

public class ReplacementResult
{
    public string ScanId { get; set; } = string.Empty;
    public int TotalReplacements { get; set; }
    public int SuccessfulReplacements { get; set; }
    public int FailedReplacements { get; set; }
    public List<ReplacementDetail> Details { get; set; } = new();
    public bool AllSuccessful => FailedReplacements == 0;
}

public class ReplacementDetail
{
    public string OriginalFileId { get; set; } = string.Empty;
    public string OriginalPath { get; set; } = string.Empty;
    public string ShortcutPath { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
