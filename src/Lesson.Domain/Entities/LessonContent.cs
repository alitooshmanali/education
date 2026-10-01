using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>
/// Binary content storage (video, audio, image, document).
/// Organized under one Folder; can be visible in multiple Levels via ContentLevels.
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

    /// <summary>Levels where this content is visible. At least one required.</summary>
    public ICollection<ContentLevel> Levels { get; set; } = new List<ContentLevel>();

    public ICollection<ContentProgress> ProgressRecords { get; set; } = new List<ContentProgress>();
    public ICollection<ContentFeedback> Feedbacks { get; set; } = new List<ContentFeedback>();
}
