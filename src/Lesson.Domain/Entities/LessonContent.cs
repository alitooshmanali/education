using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>
/// Unified content storage for video, audio, image, PDF, Word, etc.
/// Binary content is stored in the database as bytes (no physical filesystem folders).
/// </summary>
public sealed class LessonContent : BaseEntity
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string FileExtension { get; set; } = string.Empty;
    public string MimeType { get; set; } = "application/octet-stream";
    public long FileSize { get; set; }
    public ContentType Type { get; set; } = ContentType.Other;
    public double? DurationSeconds { get; set; }

    public long FolderId { get; set; }
    public Folder? Folder { get; set; }

    public ICollection<ContentProgress> ProgressRecords { get; set; } = new List<ContentProgress>();
}
