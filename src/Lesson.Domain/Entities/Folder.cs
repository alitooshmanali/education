using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

public sealed class Folder : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public long? ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }
    public ICollection<Folder> ChildFolders { get; set; } = new List<Folder>();
    public ICollection<Video> Videos { get; set; } = new List<Video>();
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    public string RelativePath { get; set; } = string.Empty;
}
