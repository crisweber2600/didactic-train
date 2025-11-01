namespace SharePointDeduplicator.API.Models;

public class DuplicateGroup
{
    public string Hash { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public List<FileInfo> Files { get; set; } = new();
    public long TotalWastedSpace => (Files.Count - 1) * FileSize;
    public FileInfo? SelectedTrueCopy { get; set; }
}
