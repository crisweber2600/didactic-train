namespace SharePointDeduplicator.API.Models;

public class VerificationResult
{
    public string ScanId { get; set; } = string.Empty;
    public DateTime VerificationDate { get; set; } = DateTime.UtcNow;
    public int TotalShortcutsChecked { get; set; }
    public int ValidShortcuts { get; set; }
    public int BrokenShortcuts { get; set; }
    public List<BrokenShortcutDetail> BrokenDetails { get; set; } = new();
    public bool AllValid => BrokenShortcuts == 0;
}

public class BrokenShortcutDetail
{
    public string ShortcutPath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
}
