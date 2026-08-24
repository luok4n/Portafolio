# Portfolio API

Read-only ASP.NET Core API serving the portfolio content in English and Spanish.

Architecture and the reasoning behind it: [ADR-0004](../../../docs/adr/0004-backend-architecture.md).

## Run it

```bash
dotnet run --project Portfolio.Api --urls http://localhost:5080
```

In `Development` the API reference is at `http://localhost:5080/docs` and the OpenAPI document at
`/openapi/v1.json`. Neither is exposed outside Development.

```bash
dotnet test
```

## Projects

```text
Portfolio.Domain          entities, value objects, the tenure rule — no framework references
Portfolio.Application     use cases, DTOs, language negotiation, IPortfolioContentSource
Portfolio.Infrastructure  JsonFileContentSource (phase 3) → EF Core + PostgreSQL (phase 4)
Portfolio.Api             HTTP, OpenAPI, health, correlation ids, problem details
Portfolio.Tests           unit tests
```

## Endpoints

| Method | Path | Notes |
|---|---|---|
| GET | `/api/content` | Everything for one language, in one call |
| GET | `/api/profile` | Includes the computed years of experience |
| GET | `/api/experience` | Newest first; concurrent roles flagged |
| GET | `/api/skills` | Category labels localised, technology names not |
| GET | `/api/projects` | With public sources |
| GET | `/api/projects/{id}` | 404 as a problem document |
| GET | `/api/education` | |
| GET | `/api/social-links` | Public links only |
| GET | `/health/live` | Process liveness — depends on nothing |
| GET | `/health/ready` | Content loads in **every** supported language |

## Language resolution

`?lang=es` → `Accept-Language` → English. Regional tags (`es-CO`) resolve to their base language and
quality values are honoured. Every response says which language it resolved and why:

```json
{ "language": { "requested": "es-CO,es;q=0.9", "resolved": "es", "resolvedFrom": "accept-header" } }
```

An unsupported or malformed value falls back rather than failing — a broken header from some client
is not a reason to refuse a public page.

## Two content sources, one contract

The API reads its content either from the files in `content/` or from PostgreSQL. Which one is a
configuration decision; nothing above `IPortfolioContentSource` can tell the difference.

```bash
docker compose up -d db
```

```bash
Portfolio__Database__Enabled=true Portfolio__Database__ConnectionString="Host=localhost;Port=5432;Database=portfolio;Username=portfolio;Password=portfolio" dotnet run --project Portfolio.Api --urls http://localhost:5081
```

File mode is not a stub — it is a working mode with nothing to install, which keeps the frontend
work and the prerender build from depending on PostgreSQL being up.

With both running, this proves they agree:

```bash
node ../../../tools/api/parity-check.mjs
```

It compares `/api/content` from both, in both languages, and fails on any difference. It has already
earned its place: the two disagreed about the order of a role's project list, which no unit test
would have caught.

## Database

The schema is 20 tables. Facts live on the base tables; translated text lives in `*_translations`
keyed by `(entity_id, language_code)`, so a third language is a data change rather than a migration.

Translations are stored **resolved**: one complete row per language, with the fallback between a
translation and the base locale decided once at seed time. A read is then a straight filter on
`language_code`, and the database cannot serve a half-translated record.

The seed reuses `JsonFileContentSource` as its loader, so the merge logic exists in exactly one
place. It is reproducible rather than incremental — the content is authored and reviewed, never
edited by users, so there is nothing in the database worth preserving that is not in `content/`. A
fingerprint of the content files is stored alongside, and a run whose fingerprint already matches
does nothing.

```bash
dotnet dotnet-ef migrations add <Name> --project Portfolio.Infrastructure --startup-project Portfolio.Infrastructure --output-dir Database/Migrations
```

## Content

Served from the repository's `content/` directory, which is **linked** into the API project rather
than copied. One copy exists, shared by the CV builder, this API and the database seed.

The years of experience are never stored. `ProfessionalTenure` unions the months covered by every
role and divides by twelve, so two concurrent roles count once — the same rule the CV builder uses,
so the two can never disagree.

Override the location with `Portfolio:Content:Path`, and the allowed CORS origins with
`Portfolio:Cors:AllowedOrigins`. An empty origin list means no cross-origin access, not any origin.

## Health check semantics

`/health/live` deliberately runs no checks: it answers "is this process wedged?". A content problem
must not make an orchestrator restart a perfectly healthy process. `/health/ready` loads the content
for every supported language, which catches the case where English works and Spanish does not — a
failure that would otherwise only ever be seen by Spanish-speaking visitors.
