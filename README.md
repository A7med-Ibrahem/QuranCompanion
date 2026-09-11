# وِرْد (Wird) — Quran Companion

Phase 0 (scaffolding) + Phase 1 (authentication) of the full build plan.

## Structure

```
backend/    ASP.NET Core Web API, Clean Architecture (Domain / Application / Infrastructure / Api)
frontend/   React 18 + Vite + TypeScript
docker-compose.yml   local Postgres for development
```

## Prerequisites

- .NET 8 SDK
- Node.js 20+
- Docker (optional, for local Postgres) or a local Postgres/SQL Server instance

## Backend setup

```bash
cd backend
docker compose -f ../docker-compose.yml up -d          # or point ConnectionStrings:Default at your own DB

dotnet tool install --global dotnet-ef                  # once, if you don't have it
cd src/QuranCompanion.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "REPLACE_WITH_A_LONG_RANDOM_SECRET"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=quran_companion;Username=postgres;Password=CHANGE_ME"

dotnet ef migrations add InitialCreate -p ../QuranCompanion.Infrastructure -s .
dotnet run
```

The API applies pending migrations automatically on startup in the `Development` environment. Swagger UI is available at `/swagger`.

To use SQL Server instead of Postgres, set `"Database:Provider": "SqlServer"` in `appsettings.json` and use a SQL Server connection string — the `Npgsql`/`SqlServer` EF providers are both already referenced.

## Frontend setup

```bash
cd frontend
npm install
cp .env.example .env.local   # set VITE_API_BASE_URL to your API's URL
npm run dev
```

## What's implemented (Phase 0 + Phase 1 + Phase 2 + Phase 3 + Phase 4 + Bookmarks + Tafsir + Search + Companions + Accountability + Groups + Dark Mode)

**Backend**
- Clean Architecture skeleton: `Domain`, `Application`, `Infrastructure`, `Api`
- ASP.NET Core Identity with a custom `ApplicationUser` (adds `WirdId`, `DisplayName`)
- Unique, shareable `WIRD-XXXXXX` id generation (collision-checked)
- JWT access tokens (15 min) + rotating opaque refresh tokens stored hashed in the DB and delivered via an `httpOnly` cookie (never exposed to JS)
- Endpoints: `register`, `login`, `refresh`, `logout`, `forgot-password`, `reset-password`, `confirm-email`, `GET/PUT me`. Password reset uses a **6-digit OTP code** (not a link) - `PasswordResetOtps` table stores only its SHA-256 hash, expires in 10 minutes, max 5 wrong-code attempts, and requesting a new code invalidates any earlier unused one
- Centralized exception middleware → clean, user-friendly JSON errors, no stack traces leaked
- Email sending abstracted behind `IEmailSender` (console/log stub for dev — swap in SendGrid/SES later without touching business logic)
- CORS configured for the Vite dev server with credentials
- **Quran (Phase 2):** `Surahs`/`Ayahs` tables, all 114 surahs' metadata seeded automatically from `QuranData/surahs.json`, Surah Al-Fatiha's 7 ayahs seeded as a working starter set, `GET /api/quran/surahs`, `GET /api/quran/surahs/{n}`, `GET /api/quran/surahs/{n}/ayahs/{a}` (all public, no auth needed to read), plus a `TanzilImporter` + `dotnet run -- import-quran <path>` command to bulk-load the remaining ~6229 ayahs from the official Tanzil Uthmani text file
- **Continue Reading (Phase 3):** `ReadingProgress` table (one row per user), `GET /api/reading-progress/me`, `PUT /api/reading-progress/me` (both `[Authorize]`)
- **Google Sign-In:** `POST /api/auth/google` verifies the Google ID token server-side (`Google.Apis.Auth`), auto-creates an account on first sign-in (email pre-confirmed since Google verified it), then issues our own JWT/refresh token exactly like a normal login
- **Daily Wird (Phase 4, now with goal-based plans):** `WirdPlans` (one row per user - type + optional custom ayah count + optional target completion date) and `WirdCompletions` (one row per user per day, DB-unique on `(UserId, CompletionDate)` so duplicate completion is impossible at the data layer) tables. `GET/PUT /api/wird/plan`, `GET /api/wird/today`, `POST /api/wird/complete`. Today's range is computed from the *exact* imported `Ayahs` table (continuing right after wherever the last completed Wird ended, wrapping back to ayah 1 after a full khatm). **Note:** "page"/"Juz"-based options use an approximate average (~10 ayahs/page, ~208 ayahs/Juz) since true Mushaf page/Juz boundaries aren't imported yet (see the Juz/page TODO below). The new `GoalBased` type (spec section 14 - "finish the Quran by a date") recalculates the daily ayah count fresh every time from *remaining ayahs ÷ remaining days*, so missing a day automatically raises the next day's portion instead of quietly falling behind
- **Bookmarks:** `Bookmarks` table, DB-unique on `(UserId, SurahNumber, AyahNumber)` so duplicates are impossible. `GET/POST /api/bookmarks`, `DELETE /api/bookmarks/{surah}/{ayah}`, `PUT /api/bookmarks/{surah}/{ayah}/note`
- **Tafsir:** `GET /api/quran/surahs/{surah}/ayahs/{ayah}/tafsir` (public, no auth). Pulls from the free `api.quran-tafseer.com` service (Tafsir Al-Muyassar - التفسير الميسر) behind an `ITafsirProvider` abstraction so another source/book can be added later without touching the service, controller, or frontend. Results are cached in a `TafsirCache` table so repeat views don't re-hit the external API, and a short 8s timeout + graceful fallback means a slow/unreachable provider returns a clean "غير قادر على تحميل التفسير الآن" error instead of hanging
- **Search:** `GET /api/quran/search?q=...` (public). Matches surah names (Arabic and English) and ayah text. Arabic search normalization exactly per spec section 9 - أ/إ/آ→ا, ى→ي, diacritics stripped - done via a dedicated `ArabicTextNormalizer` that also tracks a position map, so a match found in the normalized text is highlighted at the *correct* spot in the original, fully-vocalized Ayah text. The original `Ayah.Text` is never modified; normalization only feeds a separate `NormalizedText` column used purely for matching
- **Companions (spec section 2):** `Connections` table - one row per unordered pair of users ever, DB-unique so a duplicate connection is impossible. `POST /api/companions/request` (by Wird ID; blocks self-connect, duplicate/already-connected with clear messages), `POST /{id}/accept`, `POST /{id}/reject`, `DELETE /{id}` (remove), `GET /api/companions` + `/requests/incoming` + `/requests/outgoing`. All companion data is private to the two users involved - no public profiles or activity feeds are created by this
- **Accountability, Streaks, Encouragement (spec sections 6-8):** `PrivacySettings` (one row per user - defaults to sharing only a plain completion checkmark, everything else opt-in) and `Encouragement` (a closed whitelist of 4 predefined messages, only sendable between accepted companions) tables. `GET/PUT /api/privacy-settings`, `GET /api/companions/{id}/status` (returns only what that companion's own privacy settings allow), `GET /api/encouragements/messages`, `GET /api/encouragements/received`, `POST /api/companions/{id}/encourage`. Personal streak (`GET /api/wird/streak`) counts consecutive days *you* completed your Wird, with a monthly grace: up to 5 missed days per calendar month don't break it (the 6th miss in that month does) - same forgiving mechanic as the "streak freeze" in popular habit apps, recomputed on the fly with nothing extra stored. Shared streak counts consecutive days *both* companions had a safe day (completed, or covered by their own independent monthly freeze allowance) - one person running low on freezes never affects the other's quota
- **Groups:** `Group`/`GroupMember` tables. A group can only ever be grown by an existing member adding one of *their own* accepted companions (checked server-side against the `Connections` table) - no open invites or discovery. `GET/POST /api/groups`, `GET /api/groups/{id}`, `POST /api/groups/{id}/members`, `DELETE /api/groups/{id}/members/{userId}` (leave or, if you're the creator, remove anyone), `DELETE /api/groups/{id}` (creator only). Each member's completion/streak in the group view is filtered through their own privacy settings exactly like the 1-to-1 companion status
- **Shared reading goals (spec section 15):** `SharedGoals` table - one active "finish the Quran together by this date" target per group (creator sets it via `PUT /api/groups/{id}/goal`). Progress is shown per member as their own % of the whole Quran ever completed via their Wird history - a personal figure only, never a leaderboard or ranking between members

**Frontend**
- Vite + React + TypeScript, path alias `@/`
- Design tokens (light/dark) matching the calm, manuscript-inspired brief — see `src/styles/tokens.css`
- `AuthContext` with silent refresh on load, access token kept in memory only (not localStorage) to reduce XSS exposure
- Pages: Login, Register, Forgot password (now a single two-step OTP flow: request code → enter code + new password, with a resend option), Profile (shows the user's Wird ID), Home
- Axios client with automatic access-token refresh on 401
- **Quran reader (Phase 2, now page-based):** `/quran` surah index grid (Arabic name, English name, ayah count, Meccan/Medinan), `/quran/:surahNumber` reader — full RTL, Uthmani-style typography, ayah-number markers, font-size controls (أ- / أ+). Reads as discrete pages instead of one long scroll: prev/next buttons, a page-dot indicator, and left/right swipe on touch devices; reaching the end/start of a surah's pages continues straight into the next/previous surah. Page breaks target **15 lines per page** (matching the traditional Mushaf line count) by actually measuring rendered text height live in the browser via a hidden clone of the reader (`paginateByLines`) - not real Mushaf page numbers, since those need official page-break data we don't have imported (see note below). Recalculates automatically on font-size change or window resize, keeping your place on the same ayah
- **Continue Reading (Phase 3):** the reader watches which ayah is on screen with an `IntersectionObserver` and saves it (debounced, best-effort) as the user scrolls; the Home page shows a "متابعة القراءة" card with the last surah/ayah that jumps straight there and scrolls/highlights it
- **Google Sign-In:** "Continue with Google" button (Google Identity Services) on Login and Register - one tap creates or signs into the account, no password needed
- **Daily Wird (Phase 4, now with goal-based plans):** `/wird/settings` to pick a plan (8 options incl. custom ayah count and the new date-based goal with 30/60/90-day quick-picks or a custom date), a Wird card on Home showing today's exact Surah/Ayah range with "ابدأ القراءة" (jumps straight into the reader at the right ayah), "تم الإنجاز" (marks complete, button then disappears and the card turns green), and for goal-based plans a "🎯 متبقي X يوم" progress note
- **Bookmarks:** click any ayah's number marker in the reader to open an action menu (نسخ / حفظ الآية / التفسير). Bookmarked ayahs show a filled marker. `/bookmarks` lists everything saved, with per-ayah private notes (add/edit/delete) and a "الذهاب للآية" link straight back into the reader
- **Tafsir:** "التفسير" in the same ayah action menu opens a modal with the ayah quoted, the Tafsir text clearly separated below it, and the source named at the bottom - loading and error states handled, never blocks the reading UI
- **Search:** `/search` page (linked from the Quran index and the reader header) - one box searches both surah names and ayah text at once, results grouped into "السور" and "الآيات" sections, matching ayah text highlighted, every result clickable straight into the reader at that exact ayah
- **Companions:** `/companions` - shows your own shareable Wird ID, a box to send a request by another user's Wird ID, incoming requests (accept/reject), outgoing pending requests, and your connected companions list (remove)
- **Accountability, Streaks, Encouragement:** personal 🔥 streak badge on the Home Wird card; `/privacy-settings` with 4 clear toggles (defaults: only completion status on); each companion in `/companions` has a "عرض الحالة" panel showing whatever they've chosen to share plus predefined encouragement buttons; a "رسائل وصلتك 🤍" feed on Home shows the last 5 encouragements received
- **Groups:** `/groups` to create a named group (you're auto-added) and see all your groups; `/groups/:id` to add members (must already be your accepted companions), see everyone's today-completion and streak (respecting each person's own privacy settings), leave, remove members (creator only), or delete the group (creator only)
- **Shared reading goals:** in `/groups/:id`, the creator can set/edit a shared target date (30/60/90-day quick-picks or a custom date) shown as a "🎯 الهدف المشترك" banner with days remaining; each member's card shows their own progress bar toward finishing the whole Quran (their own pace, no comparison between members)
- **Dark mode:** a light/dark/system toggle on `/profile`, persisted in `localStorage` and applied via an inline script before React even mounts (no flash of the wrong theme on load). Every color in the app already ran through the CSS custom properties from day one, so this was purely a switch, not a redesign
- **Offline support / PWA (spec section 17):** installable (manifest + icons), via `vite-plugin-pwa`. Once you've opened a surah or a Tafsir while online, its exact text is cached and stays readable with no connection - the original text is never altered, caching just stores an exact copy of what the API returned. Marking a Wird complete or saving your reading position while offline is queued (Background Sync) and sent automatically the instant the connection returns, instead of being lost; a small banner appears at the top of the app whenever you're offline. **Testable directly via `npm run dev`** (dev-mode PWA is enabled) - no production build needed

## Importing the full Quran text (Phase 2 follow-up)

Only Al-Fatiha is seeded automatically so the reader works out of the box. To load the rest of the Quran:

1. Go to **https://tanzil.net/download/**
2. Choose **Uthmani** text type, and the **"Simple text (with aya numbers)"** format
3. Download the `.txt` file and place it anywhere on disk, e.g. `D:\quran-uthmani.txt`
4. Make sure the app has run at least once in `Development` (so the 114 surahs are seeded)
5. From `backend/src/QuranCompanion.Api`, run:
   ```
   dotnet run -- import-quran "D:\quran-uthmani.txt"
   ```
6. It prints how many ayahs were inserted. Re-running it is safe — already-imported ayahs are skipped.

## Not yet implemented (later phases per the 11-phase plan)

Ramadan mode, exact Juz/page metadata on ayahs (columns exist, populated only via a richer Tanzil export - Wird's page/Juz options use averages until then, and the reader's "15 lines per page" is measured live rather than matching the real 604 official Mushaf page breaks for the same reason).

## Environment variables reference

| Variable | Where | Purpose |
|---|---|---|
| `ConnectionStrings:Default` | backend | DB connection string |
| `Database:Provider` | backend | `Postgres` or `SqlServer` |
| `Jwt:Secret` | backend | HMAC signing key, 32+ random chars, keep out of source control |
| `Jwt:Issuer` / `Jwt:Audience` | backend | JWT validation |
| `Google:ClientId` | backend | validates the audience of Google ID tokens sent from the frontend |
| `Cors:AllowedOrigins` | backend | frontend origin(s) allowed to call the API with credentials |
| `VITE_API_BASE_URL` | frontend | base URL of the API |
| `VITE_GOOGLE_CLIENT_ID` | frontend | must match `Google:ClientId` on the backend |

## A note on this environment

This code was written and organized here, but the sandbox that produced it has no .NET SDK and no internet access, so the backend could not actually be compiled or migration-generated in this session — please run `dotnet build` and `dotnet ef migrations add` yourself locally as the first sanity check. The frontend structure follows standard Vite conventions and should `npm install && npm run dev` cleanly.
