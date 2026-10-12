# CampusEats API

ASP.NET Core 8 Web API for CampusEats (menu + JWT auth).

## Required environment variables

These are **not** stored in this repo. Set them on your host, or locally via
`dotnet user-secrets` (already configured for this machine).

| Variable | Purpose | Local example |
|---|---|---|
| `ConnectionStrings__Default` | Postgres connection string | `Host=localhost;Database=campuseats;Username=postgres;Password=postgres` |
| `Jwt__Key` | JWT signing key (HS256, needs >= 32 chars) | any long random string |
| `Jwt__Issuer` | Token issuer (defaults in `appsettings.json`) | `CampusEats` |
| `Jwt__Audience` | Token audience (defaults in `appsettings.json`) | `CampusEatsUsers` |
| `Cors__AllowedOrigins__0` | Extra allowed frontend origin (optional) | `https://campuseats-dashboard-three.vercel.app` |

`Jwt:Key` and `ConnectionStrings:Default` are intentionally empty in
`appsettings.json`. The app fails fast at startup if `Jwt:Key` is missing.

### Local setup

```bash
dotnet user-secrets set "Jwt:Key" "<your key>"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=campuseats;Username=postgres;Password=postgres"
dotnet ef database update
dotnet run
```

## Migrations

Migrations are **not** applied automatically on startup. The app seeds
`MenuItems` during boot, so it will crash against an empty database. Apply
migrations first:

```bash
dotnet ef database update
```

## Docker

```bash
docker build -t campuseats-api .
docker run -p 8080:8080 -e ConnectionStrings__Default="..." -e Jwt__Key="..." campuseats-api
```

The image listens on `0.0.0.0:8080` (see `Dockerfile`).

## CORS

Allowed origins come from `Cors:AllowedOrigins` in `appsettings.json`, plus any
hostname ending in `-xetuwizs-projects.vercel.app` so that Vercel's per-deploy
URLs keep working. Unrelated tenants are rejected.