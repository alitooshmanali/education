using Lesson.Web.Models;
using Lesson.Web.Services;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Options;
using Lesson.Infrastructure.Persistence;
using Lesson.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

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
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
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
builder.Services.AddScoped<IPlacementGameBankService, PlacementGameBankService>();
builder.Services.AddScoped<ISiteCourseService, SiteCourseService>();
builder.Services.AddScoped<ISiteBookService, SiteBookService>();
builder.Services.AddScoped<TeacherSessionState>();
builder.Services.AddScoped<Lesson.Web.Services.MediaAssetCatalog>();

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
app.MapGet("/api/site/library", async (int? category, string? cat, int? skip, int? take, long? excludeId, ISiteLibraryService lib) =>
{
    SiteLibraryCategory? parsed = null;
    if (category is int ci && Enum.IsDefined(typeof(SiteLibraryCategory), ci))
        parsed = (SiteLibraryCategory)ci;
    else if (!string.IsNullOrWhiteSpace(cat) && Enum.TryParse<SiteLibraryCategory>(cat, true, out var byName))
        parsed = byName;
    // alias: music / musics → Music
    else if (!string.IsNullOrWhiteSpace(cat) && (cat.Equals("musics", StringComparison.OrdinalIgnoreCase) || cat.Equals("music", StringComparison.OrdinalIgnoreCase)))
        parsed = SiteLibraryCategory.Music;

    if (skip is not null || take is not null || excludeId is not null)
        return Results.Ok(await lib.GetPublicPageAsync(parsed, skip ?? 0, take ?? 50, excludeId));
    return Results.Ok(await lib.GetPublicAsync(parsed));
});

app.MapGet("/api/site/pages/{pageKey}", async (string pageKey, ISitePageService pages) =>
    Results.Ok(await pages.GetPublicPageAsync(pageKey)));

app.MapGet("/api/site/media/{contentId:long}", async (long contentId, ISitePageService pages, LessonDbContext db, CancellationToken ct) =>
{
    if (!await pages.IsPublicMediaAsync(contentId, ct))
        return Results.NotFound();

    // Browser audio/video players rely on byte-range requests for metadata and seeking.
    // File(byte[], ..., enableRangeProcessing: true) emits 206 + Content-Range when requested.
    var media = await db.Contents.AsNoTracking()
        .Where(x => x.Id == contentId && x.IsActive)
        .Select(x => new { x.Content, x.MimeType, x.OriginalFileName })
        .FirstOrDefaultAsync(ct);
    if (media is null || media.Content.Length == 0) return Results.NotFound();

    return Results.File(media.Content, media.MimeType, enableRangeProcessing: true);
});



app.MapPost("/api/site/translate", async (HttpRequest req, IConfiguration config) =>
{
    try
    {
        using var doc = await System.Text.Json.JsonDocument.ParseAsync(req.Body);
        var text = doc.RootElement.TryGetProperty("text", out var t) ? t.GetString() : null;
        if (string.IsNullOrWhiteSpace(text))
            return Results.BadRequest(new { error = "text required" });

        var apiKey = config["DeepL:ApiKey"] ?? config["DEEPL_API_KEY"] ?? Environment.GetEnvironmentVariable("DEEPL_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // بدون کلید: متن اصلی برگردد تا UI نشکند
            return Results.Ok(new { translated = "", skipped = true, reason = "DeepL API key not configured" });
        }

        var target = "FA";
        if (doc.RootElement.TryGetProperty("targetLang", out var tl))
            target = tl.GetString() ?? "FA";

        using var http = new HttpClient();
        using var form = new MultipartFormDataContent
        {
            { new StringContent(text), "text" },
            { new StringContent(target), "target_lang" }
        };
        // Free API: https://api-free.deepl.com/v2/translate  Pro: https://api.deepl.com/v2/translate
        var baseUrl = config["DeepL:ApiUrl"] ?? "https://api-free.deepl.com/v2/translate";
        http.DefaultRequestHeaders.Add("Authorization", "DeepL-Auth-Key " + apiKey);
        var resp = await http.PostAsync(baseUrl, form);
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            return Results.Ok(new { translated = "", error = json });

        using var parsed = System.Text.Json.JsonDocument.Parse(json);
        var translated = "";
        if (parsed.RootElement.TryGetProperty("translations", out var arr) && arr.GetArrayLength() > 0)
            translated = arr[0].GetProperty("text").GetString() ?? "";
        return Results.Ok(new { translated });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { translated = "", error = ex.Message });
    }
});

app.MapPost("/api/site/transcribe", async (HttpRequest req) =>
{
    // Placeholder: اتصال STT (مثلاً Whisper) بعداً
    return Results.Ok(new { text = "", skipped = true, reason = "STT not configured" });
});


app.MapGet("/api/site/books", async (ISiteBookService books) =>
    Results.Ok(await books.GetPublicAsync()));

app.MapGet("/api/site/courses", async (SubjectArea? subject, CourseOfferLevel? level, ISiteCourseService courses) =>
{
    var list = await courses.GetPublicAsync(subject, level, default);
    return Results.Ok(list);
});

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
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

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

app.MapGet("/api/placement/game-bank", (IPlacementGameBankService bank) => Results.Ok(bank.GetPublicBank()));

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
        return Results.Redirect($"/login?error={Uri.EscapeDataString(result.FailureReason ?? "login_failed")}", permanent: false);
    }

    if (!context.Request.Cookies.ContainsKey("lesson.device"))
    {
        context.Response.Cookies.Append("lesson.device", device, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
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
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private ClaimsPrincipal? _principal;
    private Task? _initialization;

    public CurrentUserContext(
        IHttpContextAccessor http,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _http = http;
        _authenticationStateProvider = authenticationStateProvider;
        _principal = http.HttpContext?.User;
    }

    public Task EnsureInitializedAsync()
        => _initialization ??= InitializeCoreAsync();

    private async Task InitializeCoreAsync()
    {
        // In Interactive Server components HttpContext is not guaranteed to exist
        // after the initial HTTP request. Use Blazor's circuit authentication state.
        var authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        _principal = authenticationState.User;
    }

    private ClaimsPrincipal? Principal => _principal ?? _http.HttpContext?.User;

    public long? UserId => long.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public UserRole? Role => Enum.TryParse<UserRole>(Principal?.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;
    public string? DeviceIdentifier => Principal?.FindFirstValue("lesson.device");
    public string? SessionKey => Principal?.FindFirstValue("lesson.session");
}

public sealed record PlacementStartBody(string? Phone);
