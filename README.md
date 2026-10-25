# TaskHive

A multi-tenant task management API on ASP.NET Core 8 and EF Core 8, where multiple
organisations share one database and one deployment, with strict row-level isolation
between them.

**Status:** in active development. Milestone 1 — the multi-tenancy spine and
authentication — is complete and working end to end.

## The problem it is built around

The worst bug a multi-tenant SaaS can ship is showing one customer another customer's
data. Preventing that by remembering to write `.Where(x => x.TenantId == id)` on every
query is a discipline problem, and discipline fails. TaskHive pushes isolation down into
the persistence layer so that the safe path is the only path.

**Reads.** A global query filter is applied to every entity implementing `ITenantOwned`,
discovered by reflecting over the EF model rather than registered by hand, so a newly
added entity cannot be forgotten. The filter reads the current tenant from a `tenant_id`
claim on the JWT. With no tenant resolved the comparison is against `NULL`, which matches
nothing — unauthenticated means invisible, failing closed rather than open.

**Writes.** Query filters only affect `SELECT`, so `SaveChanges` is overridden to stamp
`TenantId` on insert, refuse any insert that has no tenant, and freeze `TenantId` on
update so a row can never be moved from one tenant to another.

The only deliberate bypass is login, which has to find a user before any tenant is known.
It uses `IgnoreQueryFilters()` on that single query, never on the context.

## Architecture

Clean Architecture, four projects, dependencies pointing inward:

    Api  ->  Infrastructure  ->  Application  ->  Domain

- **Domain** — entities and shared abstractions. References nothing at all.
- **Application** — contracts the application needs (`ITenantContext`, `ITokenService`).
  No EF Core reference, so persistence concerns cannot leak into business logic; a stray
  `using Microsoft.EntityFrameworkCore` here would not compile.
- **Infrastructure** — EF Core lives here and only here: `DbContext`, Fluent API
  configurations, migrations, the design-time factory.
- **Api** — the composition root. DI wiring, JWT authentication, controllers.

## Data model notes

- `Tenant` is the one entity that is not tenant-owned — it has no `TenantId`, it *is* the
  tenant — so it carries no query filter.
- Email is unique **per tenant** (`UNIQUE (TenantId, Email)`), not globally, so two
  organisations can each have an `admin@` address. Leading the index with `TenantId` also
  makes it serve the tenant filter.
- `Tasks` deliberately has **no** foreign key to `Tenants`. It would give SQL Server two
  cascade paths into the table (`Tenant -> Tasks` and `Tenant -> Projects -> Tasks`), which
  it rejects with error 1785. `TenantId` there is denormalised so the filter never needs a
  join, and its integrity is enforced in `SaveChanges`.

## Stack

ASP.NET Core 8 · EF Core 8 · SQL Server (LocalDB in development) · JWT bearer
authentication · ASP.NET Core Identity password hashing (PBKDF2) · Swagger / OpenAPI

## Running locally

1. Set the development secrets:

       dotnet user-secrets set "Jwt:Key" "<64 random bytes, base64>" --project TaskHive.Api
       dotnet user-secrets set "Jwt:Issuer" "TaskHive" --project TaskHive.Api
       dotnet user-secrets set "Jwt:Audience" "TaskHiveClients" --project TaskHive.Api

2. Create the database:

       dotnet ef database update --project TaskHive.Infrastructure --startup-project TaskHive.Api

3. Run the API and open `/swagger`:

       dotnet run --project TaskHive.Api

4. `POST /api/auth/signup`, click **Authorize** with the returned token, then call a
   protected endpoint. Sign up a second organisation and swap tokens to watch the same
   endpoint return different data.

The JWT signing key is never committed. It lives in user secrets during development and
comes from configuration (Key Vault or App Service settings) in production.

## Roadmap

- **M1** — multi-tenancy spine, JWT authentication, EF Core global query filters *(complete)*
- **M2** — CQRS with MediatR, `IQueryable` paging / sorting / filtering, policy-based
  authorization, FluentValidation, ProblemDetails, Serilog
- **M3** — background jobs, caching, raw SQL and execution-plan tuning, health checks,
  integration tests
- **M4** — Azure App Service and Azure SQL, Key Vault via Managed Identity, GitHub Actions
- **M5** — a small Angular front end
