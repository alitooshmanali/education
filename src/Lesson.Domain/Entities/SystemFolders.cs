namespace Lesson.Domain;

/// <summary>
/// Protected system folders for public site media. Names must not be changed or deleted.
/// </summary>
public static class SystemFolders
{
    public const string PageSiteImages = "_PageSiteImages";
    public const string PageStories = "_PageStories";
    public const string PagePodcast = "_PagePodcast";
    public const string PageVideos = "_PageVideos";
    public const string PagePlacement = "_PagePlacement";

    public static readonly string[] All =
    [
        PageSiteImages,
        PageStories,
        PagePodcast,
        PageVideos,
        PagePlacement
    ];

    public static bool IsProtected(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var n = name.Trim();
        return All.Any(x => string.Equals(x, n, StringComparison.OrdinalIgnoreCase))
               || n.StartsWith("_Page", StringComparison.OrdinalIgnoreCase);
    }
}
