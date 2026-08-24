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

#### Added (content model and CV builder)
- `content/cv-source.md` — CV text extracted from the PDF, phone redacted, as the traceability
  anchor for every later claim.
- `content/profile.en.json`, `experience.en.json`, `projects.en.json`, `skills.json`,
  `education.en.json`, `social-links.json` — structured English content, the source for both the
  database seed and the CV.
- `content/content-review.md` — every difference between the extracted CV and the published
  content, with its reason and authorisation.
- `tools/cv/build-cv.mjs` — generates the CV from `content/` as HTML and converts it to PDF with
  headless Chrome, in a public variant (no phone) and a full variant.

#### Fixed
- The Universidad Tecnológica de Pereira role (Jan–Oct 2019, Software Developer, Java/Spring Boot
  and Angular on the Ministry of Health's RIAS programme) was missing from the CV. Added — it
  removes what looked like an employment gap, adds Java and Spring Boot to the skills, and adds
  public health to the summary's sectors.
- Years of experience corrected from "7+" to "8+": 102 unique months worked as of August 2026,
  computed at build time rather than hardcoded.
- Comfama identified as the Caja de Compensación Familiar de Antioquia; the Woldev engagement
  identified as the Gobernación de Risaralda institutional website on legacy PHP.

#### Open
- The UTP responsibility bullets are drafted from the author's description and await approval.
- The author's specific contribution at Comfama is not yet captured.
- No Spanish content exists yet; every translated string needs explicit approval (ADR-0001).

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
