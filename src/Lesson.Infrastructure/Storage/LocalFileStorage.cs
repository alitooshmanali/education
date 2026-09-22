using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Lesson.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly LessonStorageOptions _options;

    public LocalFileStorage(IOptions<LessonStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<StoredFileInfo> SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var target = File.Create(fullPath);
        await content.CopyToAsync(target, cancellationToken);
        var info = new FileInfo(fullPath);
        return new StoredFileInfo(relativePath, Path.GetFileName(relativePath), info.Length, info.LastWriteTimeUtc);
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        Stream stream = File.Open(GetFullPath(relativePath), FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task MoveAsync(string sourceRelativePath, string destinationRelativePath, CancellationToken cancellationToken = default)
    {
        var source = GetFullPath(sourceRelativePath);
        var destination = GetFullPath(destinationRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (File.Exists(destination))
        {
            File.Delete(destination);
        }
        File.Move(source, destination);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
        => Task.FromResult(File.Exists(GetFullPath(relativePath)));

    public async IAsyncEnumerable<StoredFileInfo> EnumerateAsync(string? prefix = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var root = NormalizeRoot();
        if (!Directory.Exists(root))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (!string.IsNullOrWhiteSpace(prefix) && !relative.StartsWith(prefix.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var info = new FileInfo(file);
            yield return new StoredFileInfo(relative, info.Name, info.Length, info.LastWriteTimeUtc);
            await Task.Yield();
        }
    }

    private string GetFullPath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized) || normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        return Path.GetFullPath(Path.Combine(NormalizeRoot(), normalized));
    }

    private string NormalizeRoot() => Path.GetFullPath(_options.RootPath);
}
