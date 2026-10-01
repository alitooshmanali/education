namespace Lesson.Domain.Entities;

/// <summary>
/// Assigns a Grammar quiz (IsLevelWide=false) to a specific student.
/// </summary>
public sealed class QuizAssignment
{
    public long QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public long UserId { get; set; }
    public User? User { get; set; }
}
