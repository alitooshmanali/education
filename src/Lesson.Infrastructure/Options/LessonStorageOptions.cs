namespace Lesson.Infrastructure.Options;

public sealed class LessonStorageOptions
{
    public string RootPath { get; set; } = @"C:\Lesson";
    public string[] AllowedExtensions { get; set; } = [".mkv", ".mp3", ".mp4"];
    public long MaximumFileSizeInBytes { get; set; } = 5L * 1024 * 1024 * 1024;
    public int BackgroundSyncIntervalMinutes { get; set; } = 60;
}
