using Lesson.Domain.Entities;
using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record StoryDto(
    long Id,
    string Title,
    string Text,
    long? ImageContentId,
    long? VoiceContentId,
    long FolderId,
    string FolderPath,
    LanguageLevel Level,
    bool IsCompleted);

public sealed record CreateStoryRequest(
    string Title,
    string Text,
    long FolderId,
    LanguageLevel Level,
    long? ImageContentId,
    long? VoiceContentId);

public interface IStoryService
{
    Task<IReadOnlyList<StoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoryDto>> GetForUserAsync(long userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoryDto>> GetByLevelAsync(LanguageLevel level, CancellationToken cancellationToken = default);
    Task<Story?> FindAsync(long id, CancellationToken cancellationToken = default);
    Task<Story> CreateAsync(CreateStoryRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(long id, CreateStoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task MarkProgressAsync(long userId, long storyId, double progressPercent, bool completed, CancellationToken cancellationToken = default);
}
