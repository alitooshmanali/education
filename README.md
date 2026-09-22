# Lesson System

Clean Architecture .NET 9 lesson management system with:

- Blazor Server host (Interactive Server)
- SQL Server + EF Core
- Cookie authentication
- Role-based authorization
- Folder-scoped permissions
- Authenticated video streaming with range support
- Local file storage abstraction
- MediatR for Application layer messaging
- Responsive UI (RTL / Persian)

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
