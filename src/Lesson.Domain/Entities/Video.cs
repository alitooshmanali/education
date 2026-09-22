using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

public sealed class Video : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public long FolderId { get; set; }
    public Folder? Folder { get; set; }
    public string FileExtension { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public ICollection<VideoWatchSession> WatchSessions { get; set; } = new List<VideoWatchSession>();
}
