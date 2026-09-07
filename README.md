# The Readers Universe

A social reading app for real-world book lovers. Track what you're reading, review what you've finished, message other readers, and borrow, lend, or sell physical books with people near you.

**Live:** [thereadersuniverse.netlify.app](https://thereadersuniverse.netlify.app) — React frontend on Netlify, ASP.NET Core API on Render.

## Features

- **Shelves** — organise your books by reading state: Currently Reading, Read, To Be Read, Did Not Finish
- **Lending & selling** — offer any book to nearby readers as *Available to Borrow* or *For Sale*, independently of its reading state
- **Browse** — discover books offered by other readers, with search and filters
- **Borrow requests** — request a book with a message; owners accept or decline, and accepting automatically declines competing requests and marks the book lent out
- **Real-time messaging** — readers can chat over a SignalR hub, so messages arrive without a refresh
- **Book data from OpenLibrary** — titles, metadata and cover images are looked up automatically, with a rate-limited backfill service that fills in missing covers over time
- **Vinted integration** — sellers link their Vinted profile; sales happen on Vinted, not in-app
- **Reviews & ratings** — star ratings and written reviews on any book
- **Profiles** — display name, bio, member-since, and a profile photo with drag-to-crop upload
- **Dashboard** — live counts of your books, nearby offers, and pending requests

## Tech stack

| Layer     | Stack                                                                                    |
| --------- | ---------------------------------------------------------------------------------------- |
| Frontend  | React 19, TypeScript, Vite, React Router 7, React Context for auth and shared state       |
| Real-time | SignalR (`@microsoft/signalr` client, `ChatHub` server-side)                              |
| Backend   | ASP.NET Core (.NET 8) Web API, Entity Framework Core, ASP.NET Identity, JWT auth, Swagger |
| Database  | PostgreSQL (Npgsql + EF Core migrations)                                                  |
| External  | OpenLibrary API for book lookup and cover images                                          |
| Testing   | xUnit + WebApplicationFactory (SQLite) backend, Vitest + React Testing Library frontend   |
| CI/CD     | GitHub Actions (backend tests, frontend lint / type-check / build / tests), Docker        |

### Notes on a couple of design decisions

**JWT over WebSockets.** Browsers can't attach an `Authorization` header to a WebSocket handshake, so the SignalR client sends its token as an `access_token` query parameter. The API's `JwtBearerEvents.OnMessageReceived` picks it up, but only for requests to `/hubs/chat` — everything else still requires a proper bearer header.

**Cover backfill is rate-limited.** OpenLibrary is a free, shared service, so cover lookups go through a limiter and a cover policy rather than firing a request per book on every page load.

**Migrations are opt-in.** They run at startup only when `RunMigrationsOnStartup` is set, so a deploy can't silently mutate a database that wasn't expecting it.

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org)
- A PostgreSQL database. The easiest zero-install option is a free [Neon](https://neon.tech) branch — create a `dev` branch off your project and copy its connection string.

### API

Configure secrets once (kept out of git via .NET user-secrets):

```bash
cd api
dotnet user-secrets set "Jwt:Key" "<any random string of 32+ characters>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your Neon dev connection string>"
```

Apply the database schema. Either set `RunMigrationsOnStartup` to `true` in `appsettings.Development.json` and let the API migrate on boot, or run migrations yourself:

```bash
dotnet ef database update
```

Then run it:

```bash
dotnet run --urls "http://localhost:5128"
```

Swagger UI is available at `http://localhost:5128/swagger` in Development, and there's a health check at `/health`.

### Frontend

```bash
npm install
npm run dev
```

The app runs at http://localhost:5173 and talks to the API at `http://localhost:5128/api`. Override with a `VITE_API_URL` env var — copy `.env.example` to `.env` to set it locally.

## Tests

```bash
dotnet test        # backend integration tests (in-memory SQLite)
npm test           # frontend component tests
```

Both suites run in CI on every push and pull request to `main`, alongside ESLint and a TypeScript build.

## Project structure

```
api/                    ASP.NET Core Web API
  Controllers/          HTTP endpoints
  Models/               EF Core entities (Book, LibraryEntry, BorrowRequest, Review, Message, AppUser)
  Data/                 DbContext and configuration
  Migrations/           EF Core migrations
  Hubs/                 SignalR ChatHub
  Services/             OpenLibrary lookup, cover sourcing, backfill limiting
  Import/               book import tooling
ReadersRealm.Api.Tests/ backend integration tests
src/                    React app
  api/                  typed API client, one module per resource
  components/           shared components (layout, book cards, modals)
  context/              auth + book state (React Context)
  pages/                routed pages
```
