# Lesson System — اساطیر آینده

Clean Architecture .NET 9 lesson management system with:

- **Website** (marketing SPA): `/site/index.html` — تمام صفحات عمومی (خانه، آکادمی، درباره ما، تماس، پلتفرم، نظام طراحی)
- **Platform** (Blazor Server): `/login`, `/admin`, `/student` — منطق دامنه و زیرساخت بدون تغییر
- تم برند (سرمه‌ای / شنی / کرمی) روی Website و Platform
- SQL Server + EF Core
- Cookie authentication
- Role-based authorization
- Folder-scoped permissions
- Authenticated video streaming with range support
- Responsive UI (RTL / Persian)

## Website routes (`/site/index.html#/...`)

| Path | Page |
|------|------|
| `#/` | خانه |
| `#/academy` | معرفی آکادمی |
| `#/academy/english` | زبان انگلیسی |
| `#/academy/computer` | کامپیوتر |
| `#/academy/music` | موسیقی |
| `#/academy/painting` | نقاشی |
| `#/about` | درباره ما |
| `#/about/teachers` | اساتید |
| `#/about/gallery` | گالری |
| `#/contact` | تماس با ما |
| `#/platform` | معرفی پلتفرم |
| `#/design` | نظام طراحی |

ورود به پلتفرم از لینک‌های «ورود به پلتفرم» به `/login` هدایت می‌شود.

## Architecture Rules

- **Domain**: Entities, Enums, Abstractions (interfaces), Security helpers. No dependencies on other layers.
- **Application**: MediatR Commands/Queries/Handlers. Only references Domain.
- **Infrastructure**: EF Core, Services implementations, Storage. Only references Domain.
- **Web**: Blazor UI, DI composition root. References Application + Infrastructure + Domain.

Inversion of Control is applied: Web depends on abstractions (Domain), Infrastructure implements them.

## Structure

```
src/
  Lesson.Domain/
    Abstractions/     # Interfaces (IUserService, IAuthenticationService, ...)
    Entities/
    Enums/
    Security/
  Lesson.Application/
    Users/Queries/   # Sample MediatR handlers
    DependencyInjection.cs
  Lesson.Infrastructure/
    Persistence/
    Services/
    Storage/
  Lesson.Web/
    Pages/
      Login/
        Login.razor
        Login.razor.css
      Admin/
        Admin.razor
        ...
    Components/
    Layout/
    wwwroot/css/     # Global + responsive styles
```

## Pages Convention

Each page lives in its own folder under `Pages/`:

- `Pages/Login/Login.razor`
- `Pages/Login/Login.razor.css` (scoped styles)
- Logic is co-located with the page.

## Run

```bash
dotnet restore
dotnet build
dotnet run --project src/Lesson.Web
```

## Configuration

`appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LessonDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "LessonStorage": {
    "RootPath": "C:\\Lesson",
    "AllowedExtensions": [".mkv", ".mp3"],
    "MaximumFileSizeInBytes": 5368709120
  }
}
```

## UI Fixes Applied

- Proper HTML document shell in `App.razor` with CSS links and viewport meta (CSS was previously not loaded).
- Reinforced icon size constraints so SVGs no longer appear oversized.
- Expanded responsive breakpoints and mobile sidebar behavior.
- Page-scoped CSS for Login and folder-per-page structure.
- MediatR registration ready for all Application services.

## Notes

- Video files are streamed through authenticated endpoints.
- Physical storage paths are never exposed to the browser.
- Student access is limited by role, device, session, and folder permission.
