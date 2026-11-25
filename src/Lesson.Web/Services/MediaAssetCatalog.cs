using Lesson.Infrastructure.Abstractions;

namespace Lesson.Web.Services;

/// <summary>Per-circuit metadata cache so multiple media pickers do not repeat the same DB query.</summary>
public sealed class MediaAssetCatalog(IContentService contents)
{
    private Task<IReadOnlyList<ContentDto>>? _itemsTask;

    public Task<IReadOnlyList<ContentDto>> GetAsync()
        => _itemsTask ??= contents.GetAllAsync();

    public async Task<IReadOnlyList<ContentDto>> RefreshAsync()
    {
        _itemsTask = contents.GetAllAsync();
        return await _itemsTask;
    }
}
