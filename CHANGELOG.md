# Changelog

All notable changes to this project are documented here.
Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); the project follows
[Conventional Commits](https://www.conventionalcommits.org/) and phase-based delivery — see
[docs/development-plan.md](docs/development-plan.md).

## [Unreleased]

### Phase 1 — Content (in progress)

#### Added
- `content/clients-research.md` — public-source background for every client and project behind the
  author's roles, each claim traceable to a cited URL and dated, per ADR-0003. Verified: Argos ONE
  (Cementos Argos), Linkvest Capital, AES Chivor / AES Colombia, the Colombian wholesale energy
  market and its operator XM, LendingFront, MVM Ingeniería de Software, Slang, Comfama and Woldev.

#### Changed
- `docs/development-plan.md` — recorded the author's answers of 2026-08-24: the employer is
  Adagetech S.A.S.; Argos ONE and Linkvest belong to it; the LendingFront / AES Chivor overlap is
  shown as parallel freelance work; the Feb–Sep 2019 gap stays visible; the author is actively
  looking; UTP graduation year is 2018; years of experience are computed automatically without
  double-counting overlapping periods.

#### Open
- A Universidad Tecnológica de Pereira engagement on the Ministry of Health's RIAS programme
  surfaced that is **absent from the CV**. Dates, role and employment relationship are unknown, so
  it is not publishable yet — it would also add public health as a sector.
- Comfama (MVM client) and the Woldev engagement with the Gobernación de Risaralda need
  confirmation before their descriptions are written.

### Phase 0 — Repository and environment (2026-08-24)

#### Added
- Git repository initialised and linked to `github.com/luok4n/Portafolio`.
- Base repository structure: `content/`, `docs/`, `infra/`, `src/`.
- `.gitignore` covering .NET, Node/Angular, secrets and the private CV source file.
- `.editorconfig` with explicit C#, TypeScript and YAML conventions.
- `README.md`, `LICENSE` (MIT for code only), this changelog.
- `docs/environment.md` — verified local toolchain versions.
- `docs/development-plan.md` — revised 13-phase plan.
- `docs/original-development-plan.md` — the untouched original plan, kept for traceability.
- ADR-0001 (bilingual content), ADR-0002 (frontend rendering), ADR-0003 (content privacy).

#### Changed
- The original plan listed multi-language support as out of scope for the MVP; it is now a
  first-class requirement. See ADR-0001.
- Kubernetes, container registry and cloud deployment moved from the middle of the plan to
  phases 12–13, behind an explicit hosting evaluation in phase 11.
- Frontend rendering changed from a plain SPA to prerendered output. See ADR-0002.
- The "Certifications" section was dropped from the MVP: the CV contains none.
