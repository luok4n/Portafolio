# Portfolio — Sebastián Vélez Ramírez

Bilingual (EN/ES) professional portfolio built as a real, production-shaped application rather than a
static one-pager: an **Angular** frontend, an **ASP.NET Core** API, **PostgreSQL** persistence, Docker,
CI/CD and — if the final evaluation justifies it — Kubernetes.

The goal is twofold: publish the portfolio itself, and make every technical decision in this
repository explainable in a Software Engineer / Senior Backend / Technical Lead interview.

> **Status:** Phase 0 — repository scaffolding. Nothing is deployed yet.

---

## Why this repo looks the way it does

| Decision | Where it is documented |
|---|---|
| Bilingual content model (EN default, ES full parity) | [ADR-0001](docs/adr/0001-bilingual-content.md) |
| Prerendered Angular (SSG) instead of SPA or Node SSR | [ADR-0002](docs/adr/0002-frontend-rendering.md) |
| What personal data is published, and what is not | [ADR-0003](docs/adr/0003-content-privacy.md) |
| Kubernetes and hosting deliberately deferred to the end | [Development plan, phases 11–13](docs/development-plan.md) |

## Planned architecture

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
├── docs/           # Architecture, decisions (ADRs), environment, development plan
├── infra/          # Docker, Kubernetes manifests, helper scripts
└── src/
    ├── frontend/   # Angular application
    └── services/   # ASP.NET Core services
```

## Getting started

Nothing to run yet — the application is created in phase 3 onwards. Verified toolchain and
required versions are documented in [docs/environment.md](docs/environment.md).

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
