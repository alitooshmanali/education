using Lesson.Domain;
using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Lesson.Infrastructure.Services;

public sealed class CandidateService : ICandidateService
{
    private readonly LessonDbContext _db;

    // کلید سؤال → ایندکس گزینهٔ صحیح (۰-based)
    private static readonly Dictionary<string, int> CorrectAnswers = new(StringComparer.OrdinalIgnoreCase)
    {
        // Vocabulary (5)
        ["v1"] = 1, ["v2"] = 2, ["v3"] = 0, ["v4"] = 3, ["v5"] = 1,
        // Listening (5) — فعلاً متنی؛ بعداً audioUrl اضافه می‌شود
        ["l1"] = 0, ["l2"] = 2, ["l3"] = 1, ["l4"] = 0, ["l5"] = 3,
        // Reading (5)
        ["r1"] = 1, ["r2"] = 0, ["r3"] = 2, ["r4"] = 1, ["r5"] = 3,
        // Writing (5) — MCQ ساختار جمله
        ["w1"] = 2, ["w2"] = 0, ["w3"] = 1, ["w4"] = 3, ["w5"] = 0,
    };

    private static readonly List<PlacementQuestionDto> Bank =
    [
        // —— Vocabulary ——
        new("v1", "Vocabulary", "Choose the correct meaning of \"abandon\":",
            ["to buy", "to leave behind", "to repair", "to celebrate"]),
        new("v2", "Vocabulary", "Which word means \"very large\"?",
            ["tiny", "narrow", "enormous", "silent"]),
        new("v3", "Vocabulary", "\"She is fluent in English\" means she…",
            ["speaks very well", "is learning", "hates English", "only reads"]),
        new("v4", "Vocabulary", "Pick the synonym of \"begin\":",
            ["finish", "delay", "ignore", "start"]),
        new("v5", "Vocabulary", "\"Reluctant\" means…",
            ["happy", "unwilling", "hungry", "loud"]),

        // —— Listening (text stand-in; replace with audio later) ——
        new("l1", "Listening", "You hear: \"The meeting starts at half past nine.\" What time is that?",
            ["9:30", "9:15", "8:30", "10:30"]),
        new("l2", "Listening", "Speaker says they will arrive \"in a quarter of an hour.\" How many minutes?",
            ["5", "30", "15", "45"]),
        new("l3", "Listening", "\"Could you speak up, please?\" means…",
            ["speak slower", "speak louder", "sit down", "leave"]),
        new("l4", "Listening", "Announcement: \"Platform 3 for the train to London.\" Where do you go?",
            ["Platform 3", "Platform 1", "Ticket office", "Bus stop"]),
        new("l5", "Listening", "\"I'll pick you up at the airport.\" The speaker will…",
            ["call you", "send a gift", "fly with you", "come and collect you"]),

        // —— Reading ——
        new("r1", "Reading", "Text: \"Libraries are free for all residents.\" Who can use the library for free?",
            ["tourists only", "all residents", "students only", "nobody"]),
        new("r2", "Reading", "\"Please do not feed the animals.\" This sign means…",
            ["do not give food to animals", "feed them twice a day", "animals are for sale", "bring your pet"]),
        new("r3", "Reading", "Email: \"The deadline has been extended by one week.\" The deadline is now…",
            ["earlier", "the same", "one week later", "cancelled"]),
        new("r4", "Reading", "\"Despite the rain, the match continued.\" What happened?",
            ["the match was cancelled", "the match went on", "it did not rain", "players left"]),
        new("r5", "Reading", "Notice: \"Office closed until further notice.\" The office is…",
            ["open today", "open tomorrow", "permanently sold", "closed for now"]),

        // —— Writing (grammar / structure) ——
        new("w1", "Writing", "Choose the correct sentence:",
            ["She go to school every day.", "She going to school every day.", "She goes to school every day.", "She gone to school every day."]),
        new("w2", "Writing", "Complete: \"If it rains, we _____ at home.\"",
            ["will stay", "stayed", "staying", "have stay"]),
        new("w3", "Writing", "Correct article: \"I saw _____ elephant at the zoo.\"",
            ["a", "an", "the a", "—"]),
        new("w4", "Writing", "Past simple of \"write\":",
            ["writed", "written", "writing", "wrote"]),
        new("w5", "Writing", "Choose the best formal closing:",
            ["Yours sincerely,", "See ya,", "Bye bye,", "Later,"]),
    ];

    public CandidateService(LessonDbContext db) => _db = db;

    public async Task<PlacementStartResult> StartAsync(string phoneNumber, CancellationToken ct = default)
    {
        var phone = NormalizePhone(phoneNumber);
        if (phone is null)
            throw new ArgumentException("شماره موبایل معتبر نیست. مثال: 09121234567");

        // اگر قبلاً آزمون کامل کرده، رکورد جدید می‌سازیم (امکان تکرار)
        var entity = new CandidateUser
        {
            PhoneNumber = phone,
            Status = CandidateStatus.Started,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.CandidateUsers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new PlacementStartResult(entity.Id, entity.PhoneNumber);
    }

    public IReadOnlyList<PlacementQuestionDto> GetQuestions() => Bank;

    public async Task<PlacementResultDto> SubmitAsync(
        long candidateId,
        IReadOnlyList<PlacementAnswerDto> answers,
        CancellationToken ct = default)
    {
        var candidate = await _db.CandidateUsers.FirstOrDefaultAsync(x => x.Id == candidateId, ct)
            ?? throw new InvalidOperationException("کاندید یافت نشد.");

        if (candidate.Status == CandidateStatus.Completed)
            throw new InvalidOperationException("این آزمون قبلاً ثبت شده است.");

        var byId = answers
            .GroupBy(a => a.QuestionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().SelectedIndex, StringComparer.OrdinalIgnoreCase);

        decimal ScoreSkill(string skill)
        {
            var qs = Bank.Where(q => q.Skill.Equals(skill, StringComparison.OrdinalIgnoreCase)).ToList();
            if (qs.Count == 0) return 0;
            var correct = 0;
            foreach (var q in qs)
            {
                if (!byId.TryGetValue(q.Id, out var sel)) continue;
                if (CorrectAnswers.TryGetValue(q.Id, out var right) && sel == right)
                    correct++;
            }
            // ۰–۱۰
            return Math.Round(10m * correct / qs.Count, 1, MidpointRounding.AwayFromZero);
        }

        var listening = ScoreSkill("Listening");
        var reading = ScoreSkill("Reading");
        var writing = ScoreSkill("Writing");
        var vocabulary = ScoreSkill("Vocabulary");

        var (total, average, level) = PlacementScoreCalculator.Compute(listening, reading, writing, vocabulary);

        candidate.ListeningScore = listening;
        candidate.ReadingScore = reading;
        candidate.WritingScore = writing;
        candidate.VocabularyScore = vocabulary;
        candidate.TotalScore = total;
        candidate.SuggestionLevel = level;
        candidate.Status = CandidateStatus.Completed;
        candidate.CompletedAt = DateTimeOffset.UtcNow;
        candidate.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new PlacementResultDto(
            candidate.Id,
            candidate.PhoneNumber,
            listening, reading, writing, vocabulary,
            total, average,
            level.ToString(),
            PlacementScoreCalculator.LevelLabel(level));
    }

    public async Task<IReadOnlyList<CandidateUserAdminDto>> GetAllAdminAsync(CancellationToken ct = default)
    {
        return await _db.CandidateUsers
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CandidateUserAdminDto(
                x.Id,
                x.PhoneNumber,
                x.ListeningScore,
                x.ReadingScore,
                x.WritingScore,
                x.VocabularyScore,
                x.TotalScore,
                x.SuggestionLevel.HasValue ? x.SuggestionLevel.Value.ToString() : null,
                x.Status.ToString(),
                x.CreatedAt,
                x.CompletedAt,
                x.Notes))
            .ToListAsync(ct);
    }

    /// <summary>نرمال‌سازی شماره ایران: فقط ارقام، تبدیل +98 / 0098 / 98 به 0</summary>
    public static string? NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var digits = Regex.Replace(raw, @"[^\d]", "");
        if (digits.StartsWith("0098")) digits = "0" + digits[4..];
        else if (digits.StartsWith("98") && digits.Length >= 12) digits = "0" + digits[2..];
        else if (digits.StartsWith("9") && digits.Length == 10) digits = "0" + digits;

        if (digits.Length != 11 || !digits.StartsWith("09")) return null;
        return digits;
    }
}
