using Lesson.Web.Services;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Options;
using Lesson.Infrastructure.Persistence;
using Lesson.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

public record GameScoreRequest(
    long CandidateId,
    decimal ListeningScore,
    decimal ReadingScore,
    decimal WritingScore,
    decimal VocabularyScore,
    string? SuggestionLevel);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5070");

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(o =>
{
    o.DetailedErrors = builder.Environment.IsDevelopment();
    o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    o.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2);
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/denied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin)));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole(nameof(UserRole.Student)));
    options.AddPolicy("TeacherOnly", policy => policy.RequireRole(nameof(UserRole.Teacher)));
    options.AddPolicy("Staff", policy => policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Teacher)));
});

builder.Services.Configure<LessonStorageOptions>(builder.Configuration.GetSection("LessonStorage"));
builder.Services.AddDbContext<LessonDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<LessonDbContext>());
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFolderService, FolderService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<Lesson.Infrastructure.Abstractions.IAuthenticationService, Lesson.Infrastructure.Services.AuthenticationService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IContentStreamingService, ContentStreamingService>();
builder.Services.AddScoped<IStoryService, StoryService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<ISiteLibraryService, SiteLibraryService>();
builder.Services.AddScoped<ISitePageService, SitePageService>();
builder.Services.AddScoped<ITeacherWorkspaceService, TeacherWorkspaceService>();
builder.Services.AddScoped<ICandidateService, CandidateService>();
builder.Services.AddScoped<ISiteCourseService, SiteCourseService>();
builder.Services.AddScoped<TeacherSessionState>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

var app = builder.Build();

// Ensure protected site folders exist
using (var scope = app.Services.CreateScope())
{
    try
    {
        var folders = scope.ServiceProvider.GetRequiredService<IFolderService>();
        await folders.EnsureSystemFoldersAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogWarning(ex, "Could not ensure system folders (DB may be unavailable).");
    }
}

app.UseExceptionHandler("/error");
app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<Lesson.Web.App>().AddInteractiveServerRenderMode();

// Public marketing website
app.MapGet("/site", () => Results.Redirect("/site/index.html"));

// Public site library API
app.MapGet("/api/site/library", async (SiteLibraryCategory? category, int? skip, int? take, long? excludeId, ISiteLibraryService lib) =>
{
    if (skip is not null || take is not null || excludeId is not null)
        return Results.Ok(await lib.GetPublicPageAsync(category, skip ?? 0, take ?? 10, excludeId));
    return Results.Ok(await lib.GetPublicAsync(category));
});

app.MapGet("/api/site/pages/{pageKey}", async (string pageKey, ISitePageService pages) =>
    Results.Ok(await pages.GetPublicPageAsync(pageKey)));

app.MapGet("/api/site/media/{contentId:long}", async (HttpContext context, long contentId, ISitePageService pages, IContentStreamingService streaming) =>
{
    if (!await pages.IsPublicMediaAsync(contentId))
        return Results.NotFound();
    // Public stream for site-linked content only
    var result = await streaming.OpenAsync(contentId, 0, string.Empty, string.Empty, isAdmin: true);
    if (result is null) return Results.NotFound();
    return Results.Stream(result.Stream, result.ContentType, enableRangeProcessing: result.Stream.CanSeek, fileDownloadName: result.FileName);
});


app.MapGet("/api/site/courses", async (SubjectArea? subject, CourseOfferLevel? level, ISiteCourseService courses) =>
    Results.Ok(await courses.GetPublicAsync(subject, level)));

app.MapGet("/api/site/courses/{id:long}", async (long id, ISiteCourseService courses) =>
{
    var item = await courses.GetByIdAsync(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
});

app.MapGet("/api/site/library/{id:long}", async (long id, ISiteLibraryService lib) =>
{
    var item = await lib.GetByIdAsync(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
});


// ---------- Auth ----------

// Placement test (public, guests)
app.MapPost("/api/placement/start", async (PlacementStartBody body, ICandidateService svc) =>
{
    try
    {
        var r = await svc.StartAsync(body.Phone ?? "");
        return Results.Ok(r);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/placement/questions", (ICandidateService svc) =>
    Results.Ok(svc.GetQuestions()));

app.MapPost("/api/placement/submit", async (PlacementSubmitRequest body, ICandidateService svc) =>
{
    try
    {
        var r = await svc.SubmitAsync(body.CandidateId, body.Answers ?? Array.Empty<PlacementAnswerDto>());
        return Results.Ok(r);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });

app.MapPost("/api/placement/submit-game", async (GameScoreRequest body, ICandidateService candidates) =>
{
    if (body.CandidateId <= 0) return Results.BadRequest(new { error = "candidateId invalid" });
    try
    {
        var result = await candidates.SubmitGameAsync(
            body.CandidateId,
            body.ListeningScore,
            body.ReadingScore,
            body.WritingScore,
            body.VocabularyScore,
            body.SuggestionLevel);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/placement/game-bank", (ICandidateService candidates) => Results.Ok(candidates.GetGameBank()));


    }
});

app.MapGet("/api/admin/candidates", async (ICandidateService svc) =>
    Results.Ok(await svc.GetAllAdminAsync())).RequireAuthorization("AdminOnly");

app.MapPost("/api/auth/login", async (HttpContext context, Lesson.Infrastructure.Abstractions.IAuthenticationService auth) =>
{
    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var device = context.Request.Cookies["lesson.device"] ?? Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    var result = await auth.LoginAsync(new LoginRequest(username, password, device));
    if (!result.Success || result.User is null || result.SessionKey is null)
    {
        return Results.Redirect($"/login?error={Uri.EscapeDataString(result.FailureReason ?? "login_failed")}", true);
    }

    if (!context.Request.Cookies.ContainsKey("lesson.device"))
    {
        context.Response.Cookies.Append("lesson.device", device, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
            IsEssential = true
        });
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
        new(ClaimTypes.Name, result.User.Username),
        new(ClaimTypes.Role, result.User.Role.ToString()),
        new("lesson.device", device),
        new("lesson.session", result.SessionKey)
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    var home = result.User.Role switch
    {
        UserRole.Admin => "/admin",
        UserRole.Teacher => "/teacher",
        _ => "/student"
    };
    return Results.Redirect(home);
});

app.MapMethods("/api/auth/logout", ["GET", "POST"], async (HttpContext context, Lesson.Infrastructure.Abstractions.IAuthenticationService auth) =>
{
    var userId = long.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    var session = context.User.FindFirstValue("lesson.session") ?? string.Empty;
    await auth.LogoutAsync(userId, session);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

// ---------- Content streaming (from DB bytes) ----------
app.MapGet("/api/contents/{id:long}/stream", async (HttpContext context, long id, IContentStreamingService streaming) =>
{
    if (!(context.User.Identity?.IsAuthenticated ?? false))
        return Results.Unauthorized();

    var userId = long.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    var device = context.User.FindFirstValue("lesson.device") ?? string.Empty;
    var session = context.User.FindFirstValue("lesson.session") ?? string.Empty;
    // Admin and Teacher can open streams without student permission checks
    var isStaff = context.User.IsInRole(nameof(UserRole.Admin))
                  || context.User.IsInRole(nameof(UserRole.Teacher));

    var result = await streaming.OpenAsync(id, userId, device, session, isStaff);
    if (result is null)
        return Results.Forbid();

    if (result.ResumePositionSeconds.HasValue)
        context.Response.Headers["X-Resume-Position"] = result.ResumePositionSeconds.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

    // Stream in chunks; enable range only when stream is seekable (avoids loading entire LOB into memory)
    var enableRange = result.Stream.CanSeek;
    if (enableRange)
        context.Response.Headers[HeaderNames.AcceptRanges] = "bytes";

    return Results.Stream(result.Stream, result.ContentType, enableRangeProcessing: enableRange, fileDownloadName: result.FileName);
}).RequireAuthorization();

// ---------- Progress update ----------
app.MapPost("/api/contents/{id:long}/progress", async (HttpContext context, long id, IContentService contents) =>
{
    if (!(context.User.Identity?.IsAuthenticated ?? false))
        return Results.Unauthorized();

    // Admin must not write progress
    if (context.User.IsInRole(nameof(UserRole.Admin)))
        return Results.NoContent();

    var userId = long.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    var body = await context.Request.ReadFromJsonAsync<ProgressUpdateRequest>();
    if (body is null)
        return Results.BadRequest();

    await contents.UpdateProgressAsync(userId, id, body.PositionSeconds, body.DurationSeconds);
    return Results.Ok();
}).RequireAuthorization();

// ---------- Admin download ----------
app.MapGet("/api/admin/contents/{id:long}/download", async (HttpContext context, long id, IContentStreamingService streaming) =>
{
    if (!context.User.IsInRole(nameof(UserRole.Admin)))
        return Results.Forbid();

    var userId = long.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    var device = context.User.FindFirstValue("lesson.device") ?? string.Empty;
    var session = context.User.FindFirstValue("lesson.session") ?? string.Empty;

    var result = await streaming.OpenAsync(id, userId, device, session, true);
    if (result is null)
        return Results.NotFound();

    return Results.File(result.Stream, result.ContentType, result.FileName);
}).RequireAuthorization();

app.Run();

public sealed record ProgressUpdateRequest(double PositionSeconds, double? DurationSeconds);

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _http;
    public CurrentUserContext(IHttpContextAccessor http) => _http = http;
    public long? UserId => long.TryParse(_http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public UserRole? Role => Enum.TryParse<UserRole>(_http.HttpContext?.User.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;
    public string? DeviceIdentifier => _http.HttpContext?.User.FindFirstValue("lesson.device");
    public string? SessionKey => _http.HttpContext?.User.FindFirstValue("lesson.session");
}

public sealed record PlacementStartBody(string? Phone);
