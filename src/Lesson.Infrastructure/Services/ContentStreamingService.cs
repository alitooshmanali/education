using System.Data;
using System.Data.Common;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

/// <summary>
/// Streams content bytes from SQL Server in chunks.
/// Never materializes the full varbinary into a managed byte[] for playback.
/// </summary>
public sealed class ContentStreamingService : IContentStreamingService
{
    private readonly LessonDbContext _db;
    private readonly IContentService _contents;

    public ContentStreamingService(LessonDbContext db, IContentService contents)
    {
        _db = db;
        _contents = contents;
    }

    public async Task<ContentStreamResult?> OpenAsync(
        long contentId,
        long userId,
        string deviceIdentifier,
        string sessionKey,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        // Metadata only — no Content column
        var meta = await _db.Contents.AsNoTracking()
            .Where(x => x.Id == contentId && x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.FolderId,
                x.MimeType,
                x.OriginalFileName,
                x.FileSize
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (meta is null)
            return null;

        if (!isAdmin)
        {
            var hasAccess = await _db.Permissions.AnyAsync(p =>
                p.UserId == userId &&
                p.IsActive &&
                p.RevokedAt == null &&
                (p.FolderId == meta.FolderId ||
                 _db.Folders.Any(f => f.Id == meta.FolderId &&
                                      f.RelativePath.StartsWith(p.Folder!.RelativePath))),
                cancellationToken);

            if (!hasAccess)
                return null;
        }

        double? resume = null;
        if (!isAdmin)
        {
            var progress = await _contents.GetProgressAsync(userId, contentId, cancellationToken);
            if (progress is not null && !progress.IsCompleted && progress.LastPositionSeconds > 1)
                resume = progress.LastPositionSeconds;
        }

        // Chunked sequential stream from SQL — not a full byte[] load into app heap via EF
        var stream = await OpenSqlContentStreamAsync(contentId, cancellationToken);
        if (stream is null)
            return null;

        return new ContentStreamResult(stream, meta.FileSize, meta.MimeType, meta.OriginalFileName, resume);
    }

    /// <summary>
    /// Opens a forward-only stream over Contents.Content using SequentialAccess.
    /// Caller must dispose the stream (which disposes reader/command).
    /// </summary>
    private async Task<Stream?> OpenSqlContentStreamAsync(long contentId, CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT [Content]
            FROM [Contents]
            WHERE [Id] = @id AND [IsActive] = 1
            """;

        var param = command.CreateParameter();
        param.ParameterName = "@id";
        param.Value = contentId;
        command.Parameters.Add(param);

        // SequentialAccess: provider can stream LOB without buffering entire value
        var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            await reader.DisposeAsync();
            await command.DisposeAsync();
            return null;
        }

        // GetStream streams LOB; OwningStream disposes reader/command when video finishes
        var dbStream = reader.GetStream(0);
        return new OwningDbStream(dbStream, reader, command);
    }

    /// <summary>
    /// Ensures DbDataReader/DbCommand are disposed when the response stream is disposed.
    /// </summary>
    private sealed class OwningDbStream : Stream
    {
        private readonly Stream _inner;
        private readonly DbDataReader _reader;
        private readonly DbCommand _command;
        private bool _disposed;

        public OwningDbStream(Stream inner, DbDataReader reader, DbCommand command)
        {
            _inner = inner;
            _reader = reader;
            _command = command;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;
            if (disposing)
            {
                _inner.Dispose();
                _reader.Dispose();
                _command.Dispose();
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await _inner.DisposeAsync();
            await _reader.DisposeAsync();
            await _command.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}
