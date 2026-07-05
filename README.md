# CareTrack LMS

Multi-tenant Learning Management System for Apollo Allied Health programmes.

## Stack

- **Backend:** ASP.NET Core 10 Web API (Clean Architecture)
- **Frontend:** React 19 + Vite + Tailwind + shadcn-style components + GSAP
- **Database:** PostgreSQL 16
- **Auth:** ASP.NET Identity + JWT
- **Cloud:** Azure (App Service, Static Web Apps, PostgreSQL Flexible Server, Blob Storage, Key Vault)

## Quick Start

### Prerequisites

- .NET 10 SDK
- Node.js 24+
- Docker (for PostgreSQL)

### 1. Start PostgreSQL

```bash
docker compose up -d
```

### 2. Run API

```bash
dotnet run --project src/CareTrack.Api
```

API: http://localhost:5193 · Swagger: http://localhost:5193/swagger

### 3. Run Frontend

```bash
cd web/caretrack-web
npm install
npm run dev
```

Web: http://localhost:5173

## Demo Accounts

| Role | Email | Password |
|------|-------|----------|
| Apollo Admin | admin@apollo.edu | Admin@123 |
| Apollo Faculty | faculty@apollo.edu | Faculty@123 |
| University Admin | admin@meridian.edu | UnivAdmin@123 |

## Project Structure

```
src/
  CareTrack.Api/           # Controllers, middleware, Program.cs
  CareTrack.Application/   # Services, DTOs, interfaces
  CareTrack.Domain/        # Entities, enums, exceptions
  CareTrack.Infrastructure/# EF Core, Identity, blob, email
web/caretrack-web/         # React frontend
tests/                     # Unit + integration tests
```

## API Versioning

All endpoints: `/api/v1/...`

## Azure Deployment

1. Create Azure PostgreSQL Flexible Server, App Service (Linux .NET 10), Storage Account, Key Vault
2. Store secrets in Key Vault: `DbConnectionString`, `JwtKey`, `BlobConnectionString`
3. Configure App Service Key Vault references (see `appsettings.Production.json`)
4. Add GitHub secrets: `AZURE_WEBAPP_PUBLISH_PROFILE`, `AZURE_STATIC_WEB_APPS_API_TOKEN`
5. Push to `main` branch — GitHub Actions deploys automatically

## Phase 2 (Deferred)

- Supervisor role
- Clinical rotations, logbook sign-off, attendance tracking
