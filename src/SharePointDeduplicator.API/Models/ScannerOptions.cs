namespace SharePointDeduplicator.API.Models;

public class ScannerOptions
{
    public const string SectionName = "Scanner";
    
    /// <summary>
    /// Maximum number of pages to scan per directory to prevent infinite loops.
    /// Default is 10000 pages (approximately 2M items at 200 items per page).
    /// </summary>
    public int MaxPagesPerDirectory { get; set; } = 10000;
}
