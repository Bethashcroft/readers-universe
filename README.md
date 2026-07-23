# The Readers Universe

A social reading app for real-world book lovers. Track what you're reading, review what you've finished, and borrow, lend, or sell physical books with readers near you.

## Features

- **Shelves** — organise your books by reading state: Currently Reading, Read, To Be Read, Did Not Finish
- **Lending & selling** — offer any book to nearby readers as *Available to Borrow* or *For Sale*, independently of its reading state
- **Browse** — discover books offered by other readers, with search and filters
- **Borrow requests** — request a book with a message; owners accept or decline, and accepting automatically declines competing requests and marks the book lent out
- **Vinted integration** — sellers link their Vinted profile; sales happen on Vinted, not in-app
- **Reviews & ratings** — star ratings and written reviews on any book
- **Profiles** — display name, bio, member-since, and a profile photo with drag-to-crop upload
- **Dashboard** — live counts of your books, nearby offers, and pending requests

## Tech stack

| Layer    | Stack                                                                 |
| -------- | --------------------------------------------------------------------- |
| Frontend | React 19, TypeScript, Vite, React Router 7                            |
| Backend  | ASP.NET Core (.NET 8) Web API, Entity Framework Core, ASP.NET Identity, JWT auth |
| Database | SQL Server Express                                                     |
| Testing  | xUnit + WebApplicationFactory (SQLite), Vitest + React Testing Library |

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

Then run it — in Development the database tables are created automatically on startup:

```bash
dotnet run --urls "http://localhost:5128"
```

### Frontend

```bash
npm install
npm run dev
```

The app runs at [http://localhost:5173](http://localhost:5173) and talks to the API at `http://localhost:5128` (override with a `VITE_API_URL` env var).

## Tests

```bash
dotnet test        # backend integration tests (in-memory SQLite)
npm test           # frontend component tests
```

## Project structure

```
api/                    ASP.NET Core Web API (controllers, models, EF migrations)
ReadersRealm.Api.Tests/ backend integration tests
src/                    React app
  api/                  typed API client, one module per resource
  components/           shared components (layout, book cards, modals)
  context/              auth + book state (React Context)
  pages/                routed pages
```
