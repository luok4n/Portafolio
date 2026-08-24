# ADR-0004 — Pragmatic Clean Architecture, minimal APIs, and one storage seam

- **Status:** Accepted
- **Date:** 2026-08-24

## Context

The development plan asks for Clean Architecture and, in the same breath, warns against ceremony:
no `IGenericRepository<T>` wrapped around Entity Framework, no CQRS machinery, no layers created
because a diagram has four boxes. Those two instructions pull in opposite directions, and the usual
result is a portfolio backend with eleven files of plumbing around one query.

The domain is genuinely small: read-only professional content in two languages. The interesting
parts are the language fallback chain, the tenure calculation, and the fact that storage changes
from JSON files (phase 3) to PostgreSQL (phase 4) without the API contract moving.

## Decision

**Four projects, each earning its place.**

| Project | Holds | Depends on |
|---|---|---|
| `Portfolio.Domain` | Entities, value objects, the tenure rule | nothing |
| `Portfolio.Application` | Use cases, DTOs, language negotiation, the storage interface | Domain |
| `Portfolio.Infrastructure` | The content source implementation | Application |
| `Portfolio.Api` | HTTP, OpenAPI, health, correlation ids, error shape | Infrastructure |

**Minimal APIs, not controllers.** The plan's real requirement is that endpoints hold no business
logic. Minimal APIs make that easier to honour than controllers: each endpoint is visibly three
lines of binding and a call into `PortfolioQueryService`, and there is nowhere convenient to hide a
rule.

**One storage seam: `IPortfolioContentSource`.** Not a repository per entity, not a generic
repository — a single interface returning the whole resolved content for one language. That is the
only shape the application ever needs, and it is exactly the boundary phase 4 crosses when
PostgreSQL replaces the JSON files.

**DTOs separate from entities.** Not ceremony: the frontend contract and the domain change for
different reasons, and a rename in the domain should not be a breaking API change.

**The tenure rule lives in the domain**, not in a controller or a Angular helper. It is the one
real business rule here, it is subtle — overlapping roles count once — and both the site and the CV
builder must agree with it forever.

**Built-in structured logging, not Serilog.** The plan allows "Serilog or an equivalent". The
built-in JSON console logger produces structured lines on stdout, which is what every container
platform collects, so Serilog would be a dependency bought for nothing. Source-generated
`LoggerMessage` delegates are used throughout, so logging costs nothing when a level is disabled.

**Content lives in `content/` and is linked into the API project, never copied.** One copy in the
repository, shared by the CV builder, the API and the phase 4 seed.

## Consequences

**Positive**
- Phase 4 changes one DI registration. Nothing above `IPortfolioContentSource` knows the difference.
- The domain has no framework references, so its rules are testable without booting anything —
  which is why the tenure and negotiation tests run in milliseconds.
- Language resolution is reported back on every response (`explicit`, `accept-header`, `fallback`),
  so a caller never has to guess which language it actually received.
- No package was added that the project does not use.

**Negative**
- Four projects for a small domain is still more structure than the feature count strictly demands.
  It is justified by the phase 4 storage swap and by the project's purpose as an architecture
  reference — but it is more than the minimum, and pretending otherwise would be dishonest.
- Returning the whole content bundle per language means no partial loading. For a payload of this
  size that is the right trade, and it stops being right the moment the content grows a blog.
- `GET /api/content` overlaps the per-section endpoints. Both exist because the site wants one call
  and a reader of the OpenAPI document wants to see the parts.

**Rejected**
- *Controllers* — more files, more indirection, and a convenient place for logic to accumulate.
- *CQRS with a mediator* — one handler class per read of a read-only site is ceremony with no payoff.
- *A repository per entity* — the content is always read whole; per-entity repositories would exist
  only to look like a pattern.
