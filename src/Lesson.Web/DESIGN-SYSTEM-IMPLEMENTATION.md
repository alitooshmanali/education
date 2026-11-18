# Asatir Design System v2 — Integration Notes

## Reference and scope

- Design reference: `asatir-uiux-design-kit.zip` — Design Spec v2.0 and 15 HTML mockups.
- Application source: the complete `education(2).zip` project.
- Stack preserved: .NET 9, Blazor Interactive Server, existing static public-site hash router, existing authentication, services, persistence, routes, and API contracts.

## Applied

### Design foundations

- Centralized the application palette, semantic colors, typography, spacing, radii, shadows, motion, layout widths, and compatibility aliases in `wwwroot/css/ds/tokens.css`.
- Removed the duplicate brand token block from `wwwroot/css/app.css`; legacy styles now consume the shared tokens.
- Tokenized the common brand and semantic color values in the legacy Blazor styles and page-level Razor styles.
- Kept the explicit stylesheet cascade in `App.razor`: legacy styles first, then tokens, base, layout, components, patterns, and responsive overrides. No CSS `@import` was introduced.
- Added a shared public typography/accessibility layer and linked the same token file from all five standalone public HTML pages.
- Removed Google Fonts runtime requests from the Blazor host and all five standalone public HTML pages. The app now uses local `@font-face` declarations for the Vazirmatn variable font and bundled Inter weights for Latin text.
- Added `scripts/Install-LocalFonts.ps1` to download the official Vazirmatn v33.003 release, copy `Vazirmatn[wght].woff2` into `wwwroot/fonts/Vazirmatn-Variable.woff2`, and preserve its OFL license beside the font. See `docs/LOCAL-FONTS.md`.
- The Vazirmatn binary itself is not included in this archive: the editing environment could not reach the GitHub release asset. Inter's four static weights and license are included. Run the font script once on a machine with access to GitHub before deployment; after that, Vazirmatn is served locally and no font CDN is used at runtime.

### Application shell and shared components

- Refined the Blazor shell, user menu, sidebar, active navigation, topbar, and responsive behavior with the shared design tokens.
- Added progress-first student dashboard styling and teacher dashboard styling using only metrics already returned by the existing services.
- Refined the existing login, content detail, quiz focus/result, admin dashboard, candidates, and management-page patterns.
- `DataTable<TItem>` now supports optional loading skeletons, meaningful empty states, and an error/retry state without breaking existing call sites.
- `SearchInput` now debounces input by 300ms by default and cancels superseded searches.
- `ConfirmDialog` now closes on Escape (unless busy), has dialog labelling, and initially focuses the cancel action.
- `ToastContainer` announces messages through `aria-live`, uses the Design Kit's 4-second default duration, and cancels superseded timers to prevent an older toast timer from closing a newer message.
- Added reusable `LoadingState` and `ErrorState` components for consistent asynchronous states on detail and form pages.
- `ConfirmDialog` now exposes its description to assistive technology and cannot be dismissed from the backdrop while an operation is busy.
- Mobile table-to-card behavior, visible focus rings, reduced-motion support, and logical RTL layout properties are retained or extended.
- Replaced physical horizontal positioning/alignment rules in the public-site stylesheet with logical properties where they affect layout; LTR text uses logical `text-align:start`.

### Public site and placement experience

- The public home page now loads the shared public design-system skin after its page stylesheet.
- The placement entry modal now follows the Kit's Ava/guide-card treatment, has an accessible label and live error region, normalizes Persian/Arabic digits, validates the same accepted mobile-number forms as the existing `CandidateService`, and behaves as a mobile bottom sheet.
- The placement modal supports Escape, Tab focus containment, and focus restoration.
- The existing four-skill placement game is visually restyled in the Kit's navy/sand “light path” language. Its existing modules, game-bank loading, listening/speech behavior, candidate creation, and submit flow are unchanged.

### Candidates and management

- `/admin/candidates` now uses shared tokens, responsive candidate cards, and data-backed summary counts for total, completed, and started candidates.
- No registration conversion, average duration, or other metrics are fabricated where the current DTO does not provide them.
- Existing admin and teacher management pages share a consistent header, surface, form, table, and semantic feedback treatment.

## Important source-to-kit differences

The Design Kit mockups describe a five-gate / 15-question placement experience and a `POST /api/placement/start`, `GET /api/placement/questions`, `POST /api/placement/submit` flow. The supplied complete project contains both those candidate endpoints and a separate public four-skill game-bank experience (`/api/placement/game-bank`, `/api/placement/submit-game`) used by the current `wwwroot/site/index.html`. The integration keeps the live four-skill game flow intact rather than replacing it with a different interaction or changing the backend contract. The candidates DTO currently exposes phone, scores, suggested level, status, and dates; it does not expose all mockup metrics, so unavailable metrics are intentionally omitted.

## Validation

- Existing Razor route declarations remain at 40; no routes were renamed or removed.
- `Lesson.Domain`, `Lesson.Infrastructure`, and `Program.cs` were left unchanged.
- All 20 CSS files parsed without syntax errors in static validation.
- All five public HTML pages parsed; every linked local stylesheet/script exists; all retain `lang="fa"` and `dir="rtl"`.
- No Google Fonts or remote font stylesheet references remain. Inter files are bundled and verified; the local Vazirmatn font stylesheet is linked, and its binary is populated by `scripts/Install-LocalFonts.ps1` (not downloaded in this restricted environment).
- The public index inline JavaScript passed `node --check`; phone normalization was checked with Latin, Persian, Arabic-Indic, and `+98` examples.
- Razor-embedded CSS blocks parsed without syntax errors.
- A .NET build and browser-level visual QA could not be run in this environment because the .NET CLI and a browser runtime are unavailable. Run `dotnet build Lesson.sln` and verify the pages at 360px, 720px, 1024px, and desktop widths before deployment.
