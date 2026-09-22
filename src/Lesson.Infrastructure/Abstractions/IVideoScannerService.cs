namespace Lesson.Infrastructure.Abstractions;

public interface IVideoScannerService
{
    Task SyncAsync(CancellationToken cancellationToken = default);
}
