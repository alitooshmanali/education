using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class QuizService : IQuizService
{
    private readonly LessonDbContext _db;
    public QuizService(LessonDbContext db) => _db = db;

    public async Task<IReadOnlyList<QuizListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var quizzes = await _db.Quizzes.AsNoTracking()
            .Include(q => q.Questions)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);

        return quizzes.Select(q => new QuizListItemDto(
            q.Id, q.Title, q.Type, q.Level, q.IsLevelWide,
            q.TimeLimitMinutes, q.TotalScore, q.Questions.Count,
            false, false, null)).ToList();
    }

    public async Task<IReadOnlyList<QuizListItemDto>> GetForStudentAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user?.Level is null) return Array.Empty<QuizListItemDto>();

        var level = user.Level.Value;

        var quizzes = await _db.Quizzes.AsNoTracking()
            .Include(q => q.Questions)
            .Include(q => q.Assignments)
            .Where(q => q.IsActive && q.Level == level &&
                (q.Type == QuizType.General || q.IsLevelWide || q.Assignments.Any(a => a.UserId == userId)))
            .OrderBy(q => q.Title)
            .ToListAsync(cancellationToken);

        var attempts = await _db.QuizAttempts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);

        var retakes = await _db.QuizRetakeGrants.AsNoTracking()
            .Where(r => r.UserId == userId && !r.IsUsed && r.IsActive)
            .Select(r => r.QuizId)
            .ToListAsync(cancellationToken);

        return quizzes.Select(q =>
        {
            var userAttempts = attempts.Where(a => a.QuizId == q.Id).ToList();
            var completed = userAttempts.Where(a => a.IsCompleted).OrderByDescending(a => a.CompletedAt).FirstOrDefault();
            var hasAttempted = completed is not null;
            var canRetake = !hasAttempted || retakes.Contains(q.Id);
            return new QuizListItemDto(
                q.Id, q.Title, q.Type, q.Level, q.IsLevelWide,
                q.TimeLimitMinutes, q.TotalScore, q.Questions.Count,
                hasAttempted, canRetake, completed?.ScoreEarned);
        }).ToList();
    }

    public async Task<QuizDetailDto?> GetDetailAsync(long quizId, bool includeCorrectAnswers, CancellationToken cancellationToken = default)
    {
        var q = await _db.Quizzes.AsNoTracking()
            .Include(x => x.Questions)
                .ThenInclude(qq => qq.Options)
            .FirstOrDefaultAsync(x => x.Id == quizId, cancellationToken);

        if (q is null) return null;

        return new QuizDetailDto(
            q.Id, q.Title, q.IntroText, q.IntroImageContentId,
            q.Type, q.Level, q.IsLevelWide, q.TimeLimitMinutes, q.TotalScore,
            q.Questions.OrderBy(qq => qq.SortOrder).Select(qq => new QuizQuestionDto(
                qq.Id, qq.SortOrder, qq.Text, qq.ImageContentId, qq.Score,
                qq.Options.OrderBy(o => o.SortOrder).Select(o => new QuizOptionDto(
                    o.Id, o.SortOrder, o.Text,
                    includeCorrectAnswers && o.IsCorrect)).ToList()
            )).ToList());
    }

    public async Task<Quiz> CreateAsync(CreateQuizRequest request, CancellationToken cancellationToken = default)
    {
        ValidateQuizRequest(request);

        var quiz = new Quiz
        {
            Title = request.Title.Trim(),
            IntroText = request.IntroText ?? "",
            IntroImageContentId = request.IntroImageContentId,
            Type = request.Type,
            Level = request.Level,
            IsLevelWide = request.Type == QuizType.General || request.IsLevelWide,
            TimeLimitMinutes = request.TimeLimitMinutes,
            TotalScore = request.TotalScore,
            FolderId = request.FolderId,
            ClassId = request.ClassId
        };

        foreach (var qr in request.Questions.OrderBy(x => x.SortOrder))
        {
            var question = new QuizQuestion
            {
                Text = qr.Text,
                ImageContentId = qr.ImageContentId,
                Score = qr.Score,
                SortOrder = qr.SortOrder
            };
            foreach (var opt in qr.Options.OrderBy(o => o.SortOrder))
            {
                question.Options.Add(new QuizOption
                {
                    Text = opt.Text,
                    IsCorrect = opt.IsCorrect,
                    SortOrder = opt.SortOrder
                });
            }
            quiz.Questions.Add(question);
        }

        if (!quiz.IsLevelWide && request.AssignedUserIds is { Count: > 0 })
        {
            foreach (var uid in request.AssignedUserIds.Distinct())
                quiz.Assignments.Add(new QuizAssignment { UserId = uid });
        }

        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync(cancellationToken);
        return quiz;
    }

    public async Task UpdateAsync(long quizId, CreateQuizRequest request, CancellationToken cancellationToken = default)
    {
        ValidateQuizRequest(request);

        var quiz = await _db.Quizzes
            .Include(q => q.Questions).ThenInclude(qq => qq.Options)
            .Include(q => q.Assignments)
            .FirstAsync(q => q.Id == quizId, cancellationToken);

        // Remove old questions/options
        foreach (var qq in quiz.Questions.ToList())
        {
            _db.QuizOptions.RemoveRange(qq.Options);
            _db.QuizQuestions.Remove(qq);
        }
        quiz.Assignments.Clear();

        quiz.Title = request.Title.Trim();
        quiz.IntroText = request.IntroText ?? "";
        quiz.IntroImageContentId = request.IntroImageContentId;
        quiz.Type = request.Type;
        quiz.Level = request.Level;
        quiz.IsLevelWide = request.Type == QuizType.General || request.IsLevelWide;
        quiz.TimeLimitMinutes = request.TimeLimitMinutes;
        quiz.TotalScore = request.TotalScore;
        quiz.FolderId = request.FolderId;

        foreach (var qr in request.Questions.OrderBy(x => x.SortOrder))
        {
            var question = new QuizQuestion
            {
                Text = qr.Text,
                ImageContentId = qr.ImageContentId,
                Score = qr.Score,
                SortOrder = qr.SortOrder
            };
            foreach (var opt in qr.Options.OrderBy(o => o.SortOrder))
            {
                question.Options.Add(new QuizOption
                {
                    Text = opt.Text,
                    IsCorrect = opt.IsCorrect,
                    SortOrder = opt.SortOrder
                });
            }
            quiz.Questions.Add(question);
        }

        if (!quiz.IsLevelWide && request.AssignedUserIds is { Count: > 0 })
        {
            foreach (var uid in request.AssignedUserIds.Distinct())
                quiz.Assignments.Add(new QuizAssignment { QuizId = quizId, UserId = uid });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(long quizId, CancellationToken cancellationToken = default)
    {
        var quiz = await _db.Quizzes.FirstAsync(q => q.Id == quizId, cancellationToken);
        _db.Quizzes.Remove(quiz);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CanStartAsync(long userId, long quizId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var quiz = await _db.Quizzes.AsNoTracking()
            .Include(q => q.Assignments)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (user?.Level is null || quiz is null || !quiz.IsActive)
            return false;

        if (quiz.Level != user.Level.Value)
            return false;

        if (quiz.Type == QuizType.Grammar && !quiz.IsLevelWide &&
            !quiz.Assignments.Any(a => a.UserId == userId))
            return false;

        var completedCount = await _db.QuizAttempts
            .CountAsync(a => a.UserId == userId && a.QuizId == quizId && a.IsCompleted, cancellationToken);

        if (completedCount == 0)
            return true;

        var hasRetake = await _db.QuizRetakeGrants
            .AnyAsync(r => r.UserId == userId && r.QuizId == quizId && !r.IsUsed && r.IsActive, cancellationToken);

        return hasRetake;
    }

    public async Task<QuizAttemptStateDto> StartAttemptAsync(long userId, long quizId, CancellationToken cancellationToken = default)
    {
        if (!await CanStartAsync(userId, quizId, cancellationToken))
            throw new InvalidOperationException("امکان شروع این آزمون وجود ندارد.");

        // Resume incomplete attempt if any
        var existing = await _db.QuizAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.QuizId == quizId && !a.IsCompleted, cancellationToken);

        if (existing is not null)
            return await BuildAttemptState(existing, cancellationToken);

        var quiz = await _db.Quizzes.AsNoTracking().FirstAsync(q => q.Id == quizId, cancellationToken);

        // Consume retake grant if needed
        var completedBefore = await _db.QuizAttempts
            .AnyAsync(a => a.UserId == userId && a.QuizId == quizId && a.IsCompleted, cancellationToken);
        if (completedBefore)
        {
            var grant = await _db.QuizRetakeGrants
                .FirstOrDefaultAsync(r => r.UserId == userId && r.QuizId == quizId && !r.IsUsed && r.IsActive, cancellationToken);
            if (grant is not null)
            {
                grant.IsUsed = true;
            }
        }

        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            UserId = userId,
            StartedAt = DateTimeOffset.UtcNow
        };
        _db.QuizAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);

        return await BuildAttemptState(attempt, cancellationToken);
    }

    public async Task<QuizAttemptStateDto?> GetActiveAttemptAsync(long userId, long quizId, CancellationToken cancellationToken = default)
    {
        var attempt = await _db.QuizAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.QuizId == quizId && !a.IsCompleted, cancellationToken);
        if (attempt is null) return null;
        return await BuildAttemptState(attempt, cancellationToken);
    }

    public async Task<SubmitAnswerResult> SubmitAnswerAsync(long userId, long attemptId, long questionId, long optionId, CancellationToken cancellationToken = default)
    {
        var attempt = await _db.QuizAttempts
            .Include(a => a.Answers)
            .Include(a => a.Quiz)
            .FirstAsync(a => a.Id == attemptId && a.UserId == userId, cancellationToken);

        if (attempt.IsCompleted)
            throw new InvalidOperationException("این آزمون به پایان رسیده است.");

        // Time check
        var elapsed = (DateTimeOffset.UtcNow - attempt.StartedAt).TotalMinutes;
        if (elapsed > attempt.Quiz!.TimeLimitMinutes)
        {
            await ForceComplete(attempt, cancellationToken);
            throw new InvalidOperationException("زمان آزمون به پایان رسیده است.");
        }

        if (attempt.Answers.Any(a => a.QuestionId == questionId))
            throw new InvalidOperationException("این سؤال قبلاً پاسخ داده شده و قابل تغییر نیست.");

        var question = await _db.QuizQuestions
            .Include(q => q.Options)
            .FirstAsync(q => q.Id == questionId && q.QuizId == attempt.QuizId, cancellationToken);

        var selected = question.Options.FirstOrDefault(o => o.Id == optionId)
            ?? throw new InvalidOperationException("گزینه نامعتبر است.");

        var correctOption = question.Options.First(o => o.IsCorrect);
        var isCorrect = selected.IsCorrect;
        var score = isCorrect ? question.Score : 0;

        var answer = new QuizAnswer
        {
            AttemptId = attemptId,
            QuestionId = questionId,
            SelectedOptionId = optionId,
            IsCorrect = isCorrect,
            ScoreEarned = score,
            AnsweredAt = DateTimeOffset.UtcNow
        };
        _db.QuizAnswers.Add(answer);
        attempt.ScoreEarned += score;
        await _db.SaveChangesAsync(cancellationToken);

        return new SubmitAnswerResult(isCorrect, score, correctOption.Id);
    }

    public async Task<QuizAttemptStateDto> CompleteAttemptAsync(long userId, long attemptId, CancellationToken cancellationToken = default)
    {
        var attempt = await _db.QuizAttempts
            .Include(a => a.Answers)
            .Include(a => a.Quiz)
            .FirstAsync(a => a.Id == attemptId && a.UserId == userId, cancellationToken);

        if (!attempt.IsCompleted)
            await ForceComplete(attempt, cancellationToken);

        return await BuildAttemptState(attempt, cancellationToken);
    }

    public async Task GrantRetakeAsync(long quizId, long userId, long adminUserId, CancellationToken cancellationToken = default)
    {
        _db.QuizRetakeGrants.Add(new QuizRetakeGrant
        {
            QuizId = quizId,
            UserId = userId,
            GrantedByUserId = adminUserId,
            IsUsed = false
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignUsersAsync(long quizId, IReadOnlyList<long> userIds, CancellationToken cancellationToken = default)
    {
        var quiz = await _db.Quizzes.Include(q => q.Assignments).FirstAsync(q => q.Id == quizId, cancellationToken);
        quiz.Assignments.Clear();
        foreach (var uid in userIds.Distinct())
            quiz.Assignments.Add(new QuizAssignment { QuizId = quizId, UserId = uid });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QuizReportRowDto>> GetReportsAsync(LanguageLevel? level, long? quizId, CancellationToken cancellationToken = default)
    {
        var q = _db.QuizAttempts.AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Quiz)
            .Where(a => a.IsCompleted)
            .AsQueryable();

        if (level.HasValue)
            q = q.Where(a => a.Quiz!.Level == level.Value);
        if (quizId.HasValue)
            q = q.Where(a => a.QuizId == quizId.Value);

        var list = await q.OrderByDescending(a => a.CompletedAt).ToListAsync(cancellationToken);

        return list.Select(a => new QuizReportRowDto(
            a.UserId, a.User?.Username ?? "", a.User?.Level,
            a.QuizId, a.Quiz?.Title ?? "",
            a.ScoreEarned, a.Quiz?.TotalScore ?? 0,
            a.IsCompleted, a.CompletedAt, a.TimeSpentSeconds)).ToList();
    }

    private async Task ForceComplete(QuizAttempt attempt, CancellationToken cancellationToken)
    {
        attempt.IsCompleted = true;
        attempt.CompletedAt = DateTimeOffset.UtcNow;
        attempt.TimeSpentSeconds = (int)(DateTimeOffset.UtcNow - attempt.StartedAt).TotalSeconds;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<QuizAttemptStateDto> BuildAttemptState(QuizAttempt attempt, CancellationToken cancellationToken)
    {
        var quiz = attempt.Quiz ?? await _db.Quizzes.AsNoTracking().FirstAsync(q => q.Id == attempt.QuizId, cancellationToken);
        var answers = attempt.Answers?.ToList() ?? await _db.QuizAnswers.AsNoTracking()
            .Where(a => a.AttemptId == attempt.Id).ToListAsync(cancellationToken);

        var elapsed = (int)(DateTimeOffset.UtcNow - attempt.StartedAt).TotalSeconds;
        var remaining = Math.Max(0, quiz.TimeLimitMinutes * 60 - elapsed);

        if (!attempt.IsCompleted && remaining <= 0)
        {
            await ForceComplete(attempt, cancellationToken);
            remaining = 0;
        }

        return new QuizAttemptStateDto(
            attempt.Id, attempt.QuizId, attempt.StartedAt,
            quiz.TimeLimitMinutes, remaining, attempt.ScoreEarned, attempt.IsCompleted,
            answers.Select(a => a.QuestionId).ToList(),
            answers.Select(a => new QuizAnswerResultDto(a.QuestionId, a.SelectedOptionId, a.IsCorrect, a.ScoreEarned)).ToList());
    }

    private static void ValidateQuizRequest(CreateQuizRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("عنوان آزمون الزامی است.");
        if (request.TimeLimitMinutes < 1)
            throw new InvalidOperationException("مدت زمان آزمون باید حداقل ۱ دقیقه باشد.");
        if (request.Questions is null || request.Questions.Count == 0)
            throw new InvalidOperationException("آزمون باید حداقل یک سؤال داشته باشد.");

        foreach (var q in request.Questions)
        {
            if (q.Options is null || q.Options.Count < 2)
                throw new InvalidOperationException("هر سؤال باید حداقل ۲ گزینه داشته باشد.");
            if (q.Options.Count > 4)
                throw new InvalidOperationException("هر سؤال حداکثر ۴ گزینه می‌تواند داشته باشد.");
            if (q.Options.Count(o => o.IsCorrect) != 1)
                throw new InvalidOperationException("هر سؤال باید دقیقاً یک پاسخ صحیح داشته باشد.");
        }

        var sumScores = request.Questions.Sum(q => q.Score);
        if (sumScores > request.TotalScore)
            throw new InvalidOperationException($"مجموع نمره سؤالات ({sumScores}) نمی‌تواند از نمره کل آزمون ({request.TotalScore}) بیشتر باشد.");
        if (sumScores <= 0)
            throw new InvalidOperationException("مجموع نمره سؤالات باید بیشتر از صفر باشد.");
    }
}
