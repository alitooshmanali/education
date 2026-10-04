using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>
/// Educational story: Text + optional Image + optional Voice (audio).
/// Image and Voice reference LessonContent rows (Type=Image / Audio).
/// </summary>
public sealed class Story : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    public long? ImageContentId { get; set; }
    public LessonContent? ImageContent { get; set; }

    public long? VoiceContentId { get; set; }
    public LessonContent? VoiceContent { get; set; }

    public long FolderId { get; set; }
    public Folder? Folder { get; set; }

    public long? ClassId { get; set; }
    public ClassRoom? Class { get; set; }
    public long? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public LanguageLevel Level { get; set; }

    public ICollection<ContentProgress> ProgressRecords { get; set; } = new List<ContentProgress>();
    public ICollection<ContentFeedback> Feedbacks { get; set; } = new List<ContentFeedback>();
}
