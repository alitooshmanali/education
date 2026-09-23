using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

/// <summary>
/// Logical group / nested group. No physical filesystem folder is created.
/// Hierarchy is stored via ParentFolderId and displayed as expandable menus.
/// </summary>
public sealed class Folder : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public long? ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }
    public ICollection<Folder> ChildFolders { get; set; } = new List<Folder>();
    public ICollection<LessonContent> Contents { get; set; } = new List<LessonContent>();
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();

    /// <summary>
    /// Logical path used for display and permission inheritance (e.g. "GroupA/SubGroupB").
    /// Not a filesystem path.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;
}
