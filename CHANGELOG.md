# Changelog

All notable changes to this project are documented here.
Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); the project follows
[Conventional Commits](https://www.conventionalcommits.org/) and phase-based delivery — see
[docs/development-plan.md](docs/development-plan.md).

## [Unreleased]

### Phase 5 — Frontend (2026-08-24)

#### Added
- `src/frontend/portfolio-web` — Angular 22, zoneless, prerendered. **27 static routes**: home,
  engineering and 11 project pages in each language, plus the locale entry point.
- Bilingual routing with translated path segments (`/en/projects/x`, `/es/proyectos/x`) generated
  from one table that the router, the language switcher, the prerender route generator and
  `hreflang` all share.
- The language switcher preserves route and anchor — `/es/proyectos/slang` goes to
  `/en/projects/slang`, verified in the browser.
- Per-locale SEO: title, description, Open Graph, self-referencing canonical, reciprocal `hreflang`
  with `x-default`, and `Person` JSON-LD.
- The engineering section: a summary on the home page and a full page with hand-written inline SVG
  diagrams for the architecture, the three flows and the data model.
- `tools/frontend/build-snapshot.mjs`, which pulls the content snapshot from the API and copies the
  redacted CVs into the frontend.
- Content and UI strings are imported into the bundle rather than fetched, so prerendering is
  deterministic and offline.

#### Fixed
- `pathFor` and `translateUrl` both dropped the leading slash: filtering blank segments out of the
  array removed the empty first element too. Produced `href="es#about"` and a canonical of
  `https://sebastianvelez.deven`. Caught by reading the prerendered HTML rather than the source.
- The "back to projects" link concatenated `#projects` into a `routerLink`, which percent-encodes
  it — `/en%23projects`, a link that silently 404s. Now uses routerLink's `fragment` input.
- Transloco's loader must return an Observable, not a plain object.

#### Verified
- With the API stopped: every page renders complete from the snapshot and shows the cached-content
  notice. With the API running: the notice is gone and content comes from the API.
- Prerendered HTML contains the real text, not an empty root element — checked in both languages.
- The engineering page renders its generated figures (52 / 20 / 8 / 5) and all five diagrams.

#### Open
- The internal labels inside the SVG diagrams are still English-only. Deferred to phase 10.

### Engineering section (2026-08-24)

Requested by the author: the site should explain how it was built — why a frontend, an API and a
database, that the author's focus is backend and the site should say so, and the full technical
documentation (diagrams, flows, tests).

#### Added
- `docs/adr/0005-engineering-section.md` — it ships with the application rather than coming from
  the API, because it describes this codebase and must never survive the architecture it describes.
  Hosting moves to ADR-0006.
- `content/engineering.{en,es}.json` — the section's copy in both languages: why the shape, where
  the engineering weight sits, architecture, six decisions each with its rejected alternative and
  its cost, three flows, the data model, testing and operations.
- `tools/engineering/collect-facts.mjs` — measures the repository and writes
  `content/engineering-facts.json`. Currently 52 tests, 20 tables, 8 endpoints, 5 accepted ADRs,
  2 languages, 6 roles, 11 projects.
- `docs/functional-design.md` gains section 4.6 and the `/en/engineering` and `/es/ingenieria`
  routes.

#### Changed
- The content validator now checks the engineering files: matching decision and flow ids across
  languages, equal item counts, every `{placeholder}` backed by a generated fact, and **no
  hardcoded counts in the prose**. Verified by deliberately breaking both rules and watching it
  fail.

### Phase 4 — PostgreSQL (2026-08-24)

#### Added
- EF Core schema of 20 tables: facts on the base tables, translated text in `*_translations` keyed
  by `(entity_id, language_code)`, so a third language is a data change and not a migration.
- Initial migration, snake_case naming, indexes, and check constraints that refuse a period ending
  before it starts or a month outside 1–12.
- `ContentSeeder`, which reuses `JsonFileContentSource` as its loader so the base-locale/translation
  merge exists in one place, and stores a fingerprint of the content files so a run that would
  change nothing does nothing.
- `EfPortfolioContentSource`, loading the whole content for a language in one pass with no-tracking
  split queries.
- `docker-compose.yml` with PostgreSQL 17 for local development.
- `tools/api/parity-check.mjs`, which compares `/api/content` from both sources in both languages.

#### Fixed
- The two content sources disagreed about the order of a role's project list: the file source used
  the array authored on the role, the database derived it from the projects' own foreign key. Both
  now derive it from the projects, so ordering has one authority. Found by the parity check, not by
  a unit test.
- EF Core package versions were split between 10.0.4 and 10.0.11, which built cleanly and then
  failed at runtime with a missing assembly. Pinned to one version.
- EF migrations are generated code and were failing the solution's warnings-as-errors; the
  `Migrations` folder is now marked generated in `.editorconfig` rather than weakening the rule for
  hand-written code.

#### Verified
- `/api/content` is byte-identical from files and from PostgreSQL, in both languages.

### Phase 3 — Backend (2026-08-24)

#### Added
- `src/services/portfolio-api` — ASP.NET Core on .NET 10, four projects: Domain, Application,
  Infrastructure, Api, plus tests. See [ADR-0004](docs/adr/0004-backend-architecture.md).
- Read endpoints resolved by locale, including a single `GET /api/content` returning the whole
  bundle, which is what both the site and the build-time snapshot actually want.
- `ProfessionalTenure` in the domain: the years of experience are computed by unioning the months
  every role covers, so concurrent roles count once. Same rule as the CV builder.
- Language negotiation: `?lang=` → `Accept-Language` (quality values and regional tags honoured) →
  English. Every response reports which language it resolved and from where.
- OpenAPI document plus a Scalar reference at `/docs`, both Development-only.
- `/health/live` (no checks — process liveness only) and `/health/ready` (content loads in every
  supported language).
- Correlation id middleware: accepted from the caller when well-formed, generated otherwise,
  echoed in the response and attached to the log scope.
- RFC 7807 problem documents from a single exception handler; only exceptions this application
  defines contribute their message, everything else stays generic.
- 52 unit tests covering the tenure rule, date handling, the negotiation fallback chain and the
  base-locale/translation merge.

#### Decided
- Minimal APIs rather than controllers: the plan's requirement is that endpoints hold no business
  logic, and minimal APIs leave nowhere convenient to hide any.
- One storage seam, `IPortfolioContentSource`, returning the whole resolved content for a language.
  No repository per entity and no generic repository. Phase 4 swaps JSON for PostgreSQL by changing
  one registration.
- Built-in JSON console logging instead of Serilog. The plan allows an equivalent, container
  platforms collect stdout, and the dependency would buy nothing.
- Content is linked into the API project from `content/`, never copied — one copy in the repository,
  shared with the CV builder and the phase 4 seed.

### Phase 2 — Functional design (2026-08-24)

#### Added
- `docs/functional-design.md` — sections and their data, the per-locale route map, language switcher
  rules, loading and fallback states, SEO metadata, UI translation key conventions, accessibility
  targets, and the API contract this implies for phase 3.

#### Decided
- Localised path segments: `/en/projects/{slug}` and `/es/proyectos/{slug}`, with a central route
  map as the single source of truth for the prerender route generator and the language switcher.
  Project slugs stay untranslated — they are proper nouns.
- The home is one anchored page; project detail pages are real routes with enough content of their
  own to justify prerendering.
- No `Accept-Language` auto-redirect: it would destabilise canonical URLs and take control away
  from the reader. Language is chosen by URL and persisted.
- The language switcher preserves route and anchor, so switching from `/en/projects/linkvest` lands
  on `/es/proyectos/linkvest` rather than the home page.
- No full-page spinner and no error screen anywhere: the prerendered content is already in the HTML,
  so the API can only improve it.
- Skills are shown without proficiency bars or numeric levels — unverifiable, and read as noise by
  a technical interviewer.
- Project detail pages link their public sources, which is what separates a verifiable claim from
  an assertion.

### Phase 1 — Content (complete)

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

#### Added (Spanish content and validation)
- `content/profile.es.json`, `experience.es.json`, `projects.es.json`, `education.es.json` — a
  complete Spanish first pass. `*.en.json` is the base locale and owns ids, dates, technologies and
  sources; the Spanish files carry only translatable fields, matched by id, so no fact is stored
  twice.
- `tools/content/validate.mjs` — fails the build when a translation is missing, has a different
  number of bullets, references an unknown project, claims `verified` without a source, or when a
  phone number appears in a tracked file.
- `cvSummary` on featured projects: a CV and a web page have different length budgets, and the
  research-length summaries pushed the CV onto a third page.

#### Resolved
- Comfama covered three pieces of work: the Slang alliance, internal management of Comfama's
  website, and a grading platform for teachers. The CV's MVM section now names Comfama alongside
  CHIVOR XM and SLANG.
- Slang's identity was flagged pending disambiguation — "SLANG" under the Comfama account could
  have been an unrelated internal project name, which would have attached another company's
  achievements to it. Confirmed as the startup: Slang and Comfama ran an alliance giving affiliates
  access to the English courses with member benefits.
- UTP responsibility bullets approved by the author.

#### Open
- Spanish translation awaits the author's approval before it is treated as final (ADR-0001).
- The CV builder emits English only; a Spanish variant is a small addition once approved.

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
