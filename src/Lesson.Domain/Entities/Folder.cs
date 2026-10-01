using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>
/// Logical group under a single LanguageLevel. No physical filesystem folder.
/// </summary>
public sealed class Folder : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Each folder belongs to exactly one level.</summary>
    public LanguageLevel Level { get; set; }

    public long? ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }
    public ICollection<Folder> ChildFolders { get; set; } = new List<Folder>();
    public ICollection<LessonContent> Contents { get; set; } = new List<LessonContent>();
    public ICollection<Story> Stories { get; set; } = new List<Story>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();

    /// <summary>Logical path for display (e.g. "Grammar/Unit1"). Not a filesystem path.</summary>
    public string RelativePath { get; set; } = string.Empty;
}
