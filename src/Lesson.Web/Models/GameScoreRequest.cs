namespace Lesson.Web.Models;

public sealed record GameScoreRequest(
    long CandidateId,
    decimal ListeningScore,
    decimal ReadingScore,
    decimal WritingScore,
    decimal VocabularyScore,
    string? SuggestionLevel);
