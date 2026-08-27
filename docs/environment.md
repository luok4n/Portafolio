# Development environment

Toolchain verified on the author's machine on **2026-08-24**. Re-verify and update this table
whenever a version is bumped; CI must pin the same major versions.

| Tool | Verified version | Target for this project | Notes |
|---|---|---|---|
| Windows | 10 Pro 19045 | — | Development host |
| Git | 2.55.0.windows.4 | ≥ 2.40 | |
| Node.js | 24.19.0 | 24.x LTS | Required by Angular 22 |
| npm | 11.17.0 | 11.x | |
| .NET SDK | 8.0.424, 9.0.317, **10.0.400** | **.NET 10 (LTS)** | 10.0.400 is the SDK used |
| Angular CLI | not installed | **22.x** (latest: 22.1.5) | Installed in phase 5 |
| Docker Engine | 29.7.2 | ≥ 27 | |
| kubectl | 1.36.1 (kustomize 5.8.1) | ≥ 1.30 | |
| Local Kubernetes cluster | none | **not needed** | Costed in [ADR-0006](adr/0006-hosting.md) and deliberately not built |
| PostgreSQL | via Docker | 17.x | No local install needed |

## Gaps

None. The Angular CLI arrived as a project dependency in phase 5 — `npm ci` in
`src/frontend/portfolio-web` installs it, so nothing has to be installed globally.

`kubectl` is present but unused: [ADR-0006](adr/0006-hosting.md) costed Kubernetes against this
workload and the decision was not to build it. Nothing in this repository needs a cluster.

## Version policy

- .NET: stay on the LTS release (.NET 10) for the whole project.
- Angular: stay on the latest stable major (22) — Angular majors ship every ~6 months and the
  repository should not showcase an outdated frontend.
- PostgreSQL: pinned by tag in `docker-compose.yml`, never `latest`.
- Docker images: pinned by digest or explicit tag in CI, never `latest` for deployments.
