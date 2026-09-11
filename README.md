<div align="center">

# 📖 Wird (وِرْد) — Quran Companion

**A full-stack web app for reading the Quran, tracking a daily Wird, and staying accountable with friends**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](#)
[![React](https://img.shields.io/badge/React-18.3-61DAFB?logo=react&logoColor=white)](#)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.6-3178C6?logo=typescript&logoColor=white)](#)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](#)
[![PWA](https://img.shields.io/badge/PWA-Offline%20Ready-5A0FC8?logo=pwa&logoColor=white)](#)
[![License](https://img.shields.io/badge/License-Add%20Yours-lightgrey)](#)

</div>

---

## 🕌 About the Project

**Wird** is a full-stack platform that combines a Mushaf-like Quran reading experience with a customizable daily reading plan (Wird) and a lightweight social accountability system — friends can encourage each other and see shared streaks, with no leaderboards or comparisons, and full user-controlled privacy.

The backend follows **Clean Architecture** with **ASP.NET Core**, and the frontend is a modern **React + TypeScript** app that works fully offline as an installable PWA.

---

## ✨ Key Features

| Area | Details |
|---|---|
| 🔐 **Authentication** | Email/password or **Google Sign-In**, JWT access tokens + rotating refresh tokens in an `httpOnly` cookie, password reset via a 6-digit OTP |
| 📚 **Quran Reader** | Full Uthmani text (114 surahs), RTL page-based reader that mimics a real Mushaf, font-size controls, automatic "continue reading" tracking |
| 🕋 **Daily Wird** | 8 plan types (custom ayah count, page-based, Juz-based, or a **date-based goal** that recalculates the daily portion automatically) |
| 🔖 **Bookmarks** | Save ayahs with private notes, jump straight back to them from the reader |
| 📝 **Tafsir** | Per-ayah tafsir from an external provider, cached locally to reduce repeat network calls |
| 🔍 **Search** | Instant search across surah names and ayah text, with full Arabic text normalization (diacritics stripped, similar letters unified) |
| 🤝 **Companions** | Connect two accounts via a unique "Wird ID", with request/accept/reject flows |
| 🔥 **Streaks & Encouragement** | Personal and shared streaks with a monthly grace allowance, predefined encouragement messages between companions only |
| 👥 **Groups** | Create groups, set shared reading goals with a target date, track each member's progress without comparison |
| 🌗 **Dark Mode** | Instant light/dark/system toggle with no flash on load |
| 📴 **Offline Support (PWA)** | Installable, caches reading/tafsir content locally, and syncs completions automatically once back online |

---

## 🏗️ Architecture

```
QuranCompanion/
├── backend/                         ASP.NET Core Web API — Clean Architecture
│   └── src/
│       ├── QuranCompanion.Domain          Core entities
│       ├── QuranCompanion.Application     Application logic and DTOs
│       ├── QuranCompanion.Infrastructure  EF Core, Identity, external services
│       └── QuranCompanion.Api             Controllers and app entry point
│
├── frontend/                        React 18 + Vite + TypeScript (PWA)
│   └── src/
│       ├── api/          API client layer (Axios)
│       ├── components/   Shared UI components
│       ├── context/      Auth / Theme / Online-status contexts
│       ├── pages/        App pages
│       ├── styles/       Design tokens (light/dark)
│       └── types/        TypeScript types
│
├── quran-uthmani.txt                Full Uthmani text (Tanzil source)
└── docker-compose.yml                Local Postgres for development
```

**Backend layering:**

```
Api  →  Application  →  Domain
  ↘             ↗
   Infrastructure
```

---

## 🧰 Tech Stack

**Backend**
- ASP.NET Core 8 (Web API) — Clean Architecture (Domain / Application / Infrastructure / Api)
- Entity Framework Core 8 — PostgreSQL or SQL Server (swappable)
- ASP.NET Core Identity + JWT Bearer Authentication
- FluentValidation
- Google.Apis.Auth (Google Sign-In)
- Swashbuckle (Swagger / OpenAPI)

**Frontend**
- React 18 + TypeScript + Vite
- React Router DOM
- Axios
- vite-plugin-pwa (Service Worker + offline support)

**Infrastructure**
- Docker Compose (PostgreSQL 16 for local development)

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- Docker (optional, for local Postgres) or your own Postgres/SQL Server instance

### 1) Backend Setup

```bash
cd backend

# Run Postgres locally (or point at your own database)
docker compose -f ../docker-compose.yml up -d

# Install the EF Core tool (once)
dotnet tool install --global dotnet-ef

cd src/QuranCompanion.Api

# Set up local secrets
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "REPLACE_WITH_A_LONG_RANDOM_SECRET"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=quran_companion;Username=postgres;Password=CHANGE_ME"

# Create the initial migration and run
dotnet ef migrations add InitialCreate -p ../QuranCompanion.Infrastructure -s .
dotnet run
```

> Pending migrations are applied automatically on startup in the `Development` environment.
> Swagger UI is available at `/swagger`.
>
> To use SQL Server instead of Postgres: set `"Database:Provider": "SqlServer"` in `appsettings.json` and use a SQL Server connection string — both the `Npgsql` and `SqlServer` EF providers are already referenced.

### 2) Frontend Setup

```bash
cd frontend
npm install
cp .env.example .env.local   # set VITE_API_BASE_URL to your API's URL
npm run dev
```

### 3) Importing the Full Quran Text

Only Surah Al-Fatiha is seeded automatically so the reader works out of the box. To load the rest of the Quran:

1. Download the text from [Tanzil](https://tanzil.net/download/) — choose the **Uthmani** text type and the **"Simple text (with aya numbers)"** format
2. Make sure the app has run at least once in `Development` (so the 114 surahs are seeded)
3. From `backend/src/QuranCompanion.Api`, run:
   ```bash
   dotnet run -- import-quran "path/to/quran-uthmani.txt"
   ```
4. It prints how many ayahs were inserted. Re-running it is safe — already-imported ayahs are skipped.

---

## 🔑 Environment Variables

| Variable | Where | Purpose |
|---|---|---|
| `ConnectionStrings:Default` | Backend | Database connection string |
| `Database:Provider` | Backend | `Postgres` or `SqlServer` |
| `Jwt:Secret` | Backend | HMAC signing key (32+ random chars), keep out of source control |
| `Jwt:Issuer` / `Jwt:Audience` | Backend | JWT validation |
| `Google:ClientId` | Backend | Validates the audience of Google ID tokens from the frontend |
| `Cors:AllowedOrigins` | Backend | Frontend origin(s) allowed to call the API with credentials |
| `VITE_API_BASE_URL` | Frontend | Base URL of the API |
| `VITE_GOOGLE_CLIENT_ID` | Frontend | Must match `Google:ClientId` on the backend |

---

## 📡 Core API Overview

| Domain | Controller | Example Routes |
|---|---|---|
| Auth | `AuthController` | `register` `login` `refresh` `logout` `forgot-password` `reset-password` `google` |
| Quran | `QuranController` | `GET /api/quran/surahs` `GET /api/quran/surahs/{n}/ayahs/{a}` `GET .../tafsir` `GET /api/quran/search` |
| Reading Progress | `ReadingProgressController` | `GET/PUT /api/reading-progress/me` |
| Daily Wird | `WirdController` | `GET/PUT /api/wird/plan` `GET /api/wird/today` `POST /api/wird/complete` `GET /api/wird/streak` |
| Bookmarks | `BookmarksController` | Add/remove/list bookmarks with notes |
| Companions | `CompanionsController` | `request` `accept` `reject` `status` `encourage` |
| Groups | `GroupsController` | CRUD for groups + shared goals |

Full interactive docs for every endpoint are available via Swagger at `/swagger` once the API is running locally.

---

## 🗺️ Roadmap (Not Yet Implemented)

- Ramadan mode
- Exact Juz/page metadata at the ayah level (columns already exist, need a richer Tanzil export) — Wird's page/Juz options currently use an average, and the reader's "15 lines per page" is measured live rather than matching the official 604 Mushaf page breaks

---

## ⚠️ A Note on This Setup

This code was written and organized in an environment with no .NET SDK and no internet access, so the backend build and migration generation were not actually verified here. Please run `dotnet build` and `dotnet ef migrations add` locally as a first sanity check. The frontend follows standard Vite conventions and should run cleanly with `npm install && npm run dev`.

---

## 📄 License

No license has been set for this project yet — add an appropriate `LICENSE` file before making it public.

