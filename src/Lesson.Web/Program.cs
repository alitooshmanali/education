using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Options;
using Lesson.Infrastructure.Persistence;
using Lesson.Infrastructure.Services;
using Lesson.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5070");

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
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
});

builder.Services.Configure<LessonStorageOptions>(builder.Configuration.GetSection("LessonStorage"));
builder.Services.AddDbContext<LessonDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<LessonDbContext>());
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFolderService, FolderService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<Lesson.Infrastructure.Abstractions.IAuthenticationService, Lesson.Infrastructure.Services.AuthenticationService>();
builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<IVideoAccessService, VideoAccessService>();
builder.Services.AddScoped<IVideoStreamingService, VideoStreamingService>();
builder.Services.AddScoped<IVideoScannerService, VideoScannerService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

var app = builder.Build();

app.UseExceptionHandler("/error");
app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<Lesson.Web.App>().AddInteractiveServerRenderMode();

app.MapPost("/api/auth/login", async (HttpContext context, Lesson.Infrastructure.Abstractions.IAuthenticationService auth, IUserService users, IConfiguration config) =>
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
        context.Response.Cookies.Append("lesson.device", device, new CookieOptions { HttpOnly = true, Secure = true, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict, IsEssential = true });
    }

    // keep explicit user/role/session claims for authorization and streaming validation

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
    return Results.Redirect(result.User.Role == UserRole.Admin ? "/admin" : "/student");
});

app.MapMethods("/api/auth/logout", ["GET", "POST"], async (HttpContext context, Lesson.Infrastructure.Abstractions.IAuthenticationService auth) =>
{
    var userId = long.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    var session = context.User.FindFirstValue("lesson.session") ?? string.Empty;
    await auth.LogoutAsync(userId, session);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.MapGet("/api/videos/{id:long}/stream", async (HttpContext context, long id, IVideoStreamingService streaming) =>
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    var userId = long.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    var device = context.User.FindFirstValue("lesson.device") ?? string.Empty;
    var session = context.User.FindFirstValue("lesson.session") ?? string.Empty;
    var isAdmin = context.User.IsInRole(nameof(UserRole.Admin));
    var result = await streaming.OpenAsync(id, userId, device, session, isAdmin);
    if (result is null)
    {
        return Results.Forbid();
    }

    context.Response.Headers[HeaderNames.AcceptRanges] = "bytes";
    return Results.Stream(result.Stream, result.ContentType, enableRangeProcessing: true, fileDownloadName: result.FileName);
}).RequireAuthorization();

app.MapGet("/api/admin/videos/{id:long}/download", async (HttpContext context, long id, IVideoStreamingService streaming) =>
{
    if (!context.User.IsInRole(nameof(UserRole.Admin)))
    {
        return Results.Forbid();
    }

    var userId = long.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    var device = context.User.FindFirstValue("lesson.device") ?? string.Empty;
    var session = context.User.FindFirstValue("lesson.session") ?? string.Empty;
    var result = await streaming.OpenAsync(id, userId, device, session, true);
    if (result is null)
    {
        return Results.NotFound();
    }

    return Results.File(result.Stream, result.ContentType, result.FileName);
}).RequireAuthorization();


app.Run();

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _http;
    public CurrentUserContext(IHttpContextAccessor http) => _http = http;
    public long? UserId => long.TryParse(_http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public UserRole? Role => Enum.TryParse<UserRole>(_http.HttpContext?.User.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;
    public string? DeviceIdentifier => _http.HttpContext?.User.FindFirstValue("lesson.device");
    public string? SessionKey => _http.HttpContext?.User.FindFirstValue("lesson.session");
}
