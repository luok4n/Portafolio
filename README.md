# Portfolio — Sebastián Vélez Ramírez

Bilingual (EN/ES) professional portfolio built as a real, production-shaped application rather than a
static one-pager: an **Angular** frontend, an **ASP.NET Core** API, **PostgreSQL** persistence, Docker,
CI/CD, and a hosting decision taken with real prices rather than assumed at the start.

The goal is twofold: publish the portfolio itself, and make every technical decision in this
repository explainable in a Software Engineer / Senior Backend / Technical Lead interview.

> **Status:** built and running locally with one command; **not deployed yet**. Hosting was decided
> with real prices in [ADR-0006](docs/adr/0006-hosting.md) — Cloudflare Pages, Cloud Run and Neon at
> $0/month — and the deploy itself, with the domain and HTTPS, is the remaining step. Kubernetes was
> costed for this workload and deliberately not built; the orchestration here is Docker Compose.

---

## Why this repo looks the way it does

| Decision | Where it is documented |
|---|---|
| Bilingual content model (EN default, ES full parity) | [ADR-0001](docs/adr/0001-bilingual-content.md) |
| Prerendered Angular (SSG) instead of SPA or Node SSR | [ADR-0002](docs/adr/0002-frontend-rendering.md) |
| Pragmatic Clean Architecture, minimal APIs, one storage seam | [ADR-0004](docs/adr/0004-backend-architecture.md) |
| The engineering section ships with the app, and its numbers are generated | [ADR-0005](docs/adr/0005-engineering-section.md) |
| What personal data is published, and what is not | [ADR-0003](docs/adr/0003-content-privacy.md) |
| Hosting at $0/month, and why Kubernetes was costed and not built | [ADR-0006](docs/adr/0006-hosting.md) |

What the project defends against, how, and what it deliberately does not do:
[docs/security.md](docs/security.md).

## Architecture

```text
                Internet
                    |
                    v
        +-----------------------+
        |  Angular (prerendered)|
        |  served by nginx      |
        +-----------------------+
                    |
                HTTPS / REST
                    |
                    v
        +-----------------------+
        |  Portfolio API        |
        |  ASP.NET Core (.NET 10)|
        +-----------------------+
                    |
                    v
        +-----------------------+
        |  PostgreSQL           |
        +-----------------------+
```

The frontend keeps a build-time snapshot of the content as a fallback, so the site never renders
empty if the API is unavailable. See [ADR-0002](docs/adr/0002-frontend-rendering.md).

## Repository layout

```text
.
├── content/        # Structured, reviewed portfolio content (EN/ES) — source for the DB seed
├── docs/           # Architecture, decisions (ADRs), security, environment, development plan
├── infra/          # Dockerfiles and nginx configuration
├── tools/          # Content validation, security scan, CV builder, generated engineering facts
└── src/
    ├── frontend/   # Angular 22, prerendered
    └── services/   # ASP.NET Core on .NET 10
```

## Run it

```bash
docker compose up --build
```

http://localhost:8080. nginx serves the prerendered site and proxies `/api` to the API, so the
browser sees one origin. Details and the container posture: [infra/README.md](infra/README.md).

For the fast edit-run loop, start only the database and run the API and frontend from the host —
see [the API](src/services/portfolio-api/README.md) and
[the frontend](src/frontend/portfolio-web/README.md).

Verified toolchain and pinned versions: [docs/environment.md](docs/environment.md).

## Checks

Every check in CI is a script that can be run locally, because a check nobody can run before pushing
is a check people learn to ignore.

```bash
node tools/content/validate.mjs
```

Translations in step, no orphaned projects, every claimed source actually cited, no phone number in a
tracked file, and no hardcoded number in the engineering section's copy.

```bash
node tools/security/scan.mjs
```

Secrets, private keys, and files that must never be tracked. Baseline, not a replacement for a real
scanner — it covers the mistakes this repository can actually make.

```bash
node tools/api/parity-check.mjs
```

With both content sources running, proves the files and PostgreSQL return byte-identical payloads.
This one has already earned its keep: it caught the two disagreeing about ordering, which no unit
test would have found.

```bash
dotnet test src/services/portfolio-api
```

[CI](.github/workflows/ci.yml) runs all of these plus the .NET build with warnings as errors, the
Angular prerender, a check that the committed content snapshot and the generated engineering figures
are still current, both container images, and a smoke test that includes stopping the API to confirm
the site still serves its content.

## Content sourcing

All professional content originates from the author's CV and is reviewed before publication.
No company, role, date, technology or achievement is invented. Client and project information is
enriched only from publicly available sources; nothing under NDA is published.

## Documentation language

Repository documentation (README, ADRs, architecture) is written in **English**, matching the
site's default language and its intended audience. Planning documents under `docs/` that are
working material for the author remain in **Spanish**.

## License

[MIT](LICENSE) for the code. The portfolio *content* (biography, experience, texts and images)
is not covered by the MIT license and may not be reused.
