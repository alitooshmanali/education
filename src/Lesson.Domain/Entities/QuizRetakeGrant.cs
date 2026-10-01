using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

/// <summary>
/// Admin grants a student permission to retake a quiz (default: one attempt only).
/// </summary>
public sealed class QuizRetakeGrant : BaseEntity
{
    public long QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public long UserId { get; set; }
    public User? User { get; set; }

    public long GrantedByUserId { get; set; }
    public User? GrantedByUser { get; set; }

    public bool IsUsed { get; set; }
}
