namespace Lesson.Web.Models;

public sealed record NavItem(string Title, string Href, string Icon, string? Badge = null);
