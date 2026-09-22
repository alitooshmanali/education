using Microsoft.AspNetCore.Components;

namespace Lesson.Web.Components;

public sealed class TableColumn<TItem>
{
    public string Header { get; set; } = string.Empty;
    public Func<TItem, RenderFragment> Template { get; set; } = _ => builder => { };
}

