namespace Lesson.Web.Models;

public sealed record ToastMessage(string Title, string Message, string Tone = "info");
