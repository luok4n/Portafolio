# Changelog

All notable changes to this project are documented here.
Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); the project follows
[Conventional Commits](https://www.conventionalcommits.org/) and phase-based delivery — see
[docs/development-plan.md](docs/development-plan.md).

## [Unreleased]

### Phase 10 — Polish (2026-08-24)

Lighthouse, mobile: **accessibility 100, best practices 100, SEO 100, performance 94–96**.

#### Added
- `sitemap.xml` and `robots.txt`, generated from the prerendered output rather than from the route
  table, so the sitemap lists exactly the pages that exist. The origin and each entry's alternates
  are read from the pages' own canonical and `hreflang` tags — the sitemap cannot disagree with the
  pages if only one of them decides.
- Social preview images, one per language, rendered from the site's own content with the headless
  browser the CV builder already needs. Without an `og:image`, LinkedIn and WhatsApp show a grey box
  with a URL, which is the first impression the link makes.
- An SVG favicon that follows the reader's theme, with the `.ico` as fallback.
- `docs/architecture.md` — context, containers, the inside of the API, the bilingual data model, how
  content reaches a reader, language resolution, operations, what is deliberately absent, and short
  answers to the interview questions the original plan lists. Diagrams are inline Mermaid so they
  cannot drift from the prose.
- `docs/diagrams/README.md` explains why there are no exported images: a binary a command cannot
  reproduce does not belong in this repository.
- Lazy-loaded routes, so reading the home page no longer downloads the engineering diagrams and the
  project detail template it may never open.

#### Fixed
- **The diagram labels were English on the Spanish page.** The gap recorded in phase 5, now closed:
  labels come from the translated content, and a page whose only untranslated text sits inside the
  engineering diagram undermines the section it illustrates.
- Two contrast failures: the availability badge at 4.39:1 and the footer's fine print at 3.32:1. The
  second came from an `opacity: 0.75` on top of the muted token — contrast now comes from the
  palette, where it can be checked, rather than from a modifier on top of it.
- The sitemap first paired pages by segment position, which cheerfully declared `/es/404` the
  Spanish counterpart of `/en/engineering`. Translated path segments share nothing to match on.

#### Changed
- The content validator compares the diagram label structure across languages, catching both a
  missing label and a mismatched number of note lines. A missing SVG label renders as blank space,
  not as an obvious gap in a sentence.
- CI now fails when prerendering logs an error. `ng build` reports success even when a route threw:
  it logs ERROR, writes a half-rendered page, and exits 0. CI also checks that every URL in the
  sitemap was actually prerendered, that no `noindex` page is listed, and that no English phrase
  from a diagram survives on the Spanish page.

### Phase 9 — Observability and security (2026-08-24)

#### Added
- `RequestLoggingMiddleware` — one structured line per request with method, **route template**,
  status, duration and correlation id. The template rather than the raw path, so a thousand requests
  for different projects aggregate into one series instead of a thousand. Health checks log at Debug:
  they run every fifteen seconds forever and at Information would bury every line that matters.
- `PortfolioMetrics` — request count, error count and a latency histogram, exposed at `/metrics` in
  Prometheus text format. Instrumented with `System.Diagnostics.Metrics`, the standard .NET API that
  OpenTelemetry consumes, so adding an exporter later changes nothing here. Aggregation and
  rendering are done in-process because there is nowhere to export to; a telemetry pipeline
  configured to scrape itself is ceremony.
- Only 5xx counts as an error. A crawler probing for `/wp-admin` generates 404s all day, and
  counting those would hide a broken deployment in the noise.
- `/metrics` is not proxied by nginx, so it is reachable only from inside the network — verified.
- Rate limiting: a fixed window per caller, 300/minute by default, partitioned by forwarded address.
  Health and metrics are **never** limited, because limiting them would let a burst of traffic
  convince an orchestrator the service is down. Rejections carry `Retry-After`.
- [`docs/security.md`](docs/security.md) — secrets, what is published, headers, CORS, input, rate
  limiting, containers, dependencies, least privilege, and an explicit section on **what this does
  not do**: no auth, no WAF, no history scanning, no CSP nonces, no HTTPS yet.
- 16 more tests, including the Prometheus rendering and the rate limiter.

#### Fixed
- **Health checks and metrics were being output-cached for five minutes.** `AddBasePolicy` with no
  predicate caches every GET, so an orchestrator would have gone on reading a stale `Healthy` after
  the service stopped being healthy, and a scraper would have recorded identical counters forever.
  Found by a metrics test whose counter refused to move. Caching now applies to `/api` only.
- The rate limit was read from configuration while the host was being built, so configuration added
  afterwards was silently ignored and the limiter appeared not to work at all. Bound through the
  options system and resolved per request instead.

### Phase 6 — Tests (2026-08-24)

**151 tests**: 89 on the backend, 62 on the frontend. Both suites run in CI.

#### Added — backend
- HTTP contract tests through the real pipeline against the real content files, so they also check
  that the published content still satisfies the contract rather than that a fixture does. Language
  negotiation, the problem-document shape, and a test asserting **no response ever contains a phone
  number** — ADR-0003 enforced at the edge, where a future content change cannot slip past it.
- Correlation id tests, including hostile input: a value with newlines, a script tag, and one 4096
  characters long. It reaches log files and a response header, so it must be sanitised, not echoed.
- `/docs` and `/openapi/v1.json` return 404 outside Development. Shipping an interactive API
  explorer publicly should be a decision, not a default.
- PostgreSQL integration tests with Testcontainers against a real database: the migration applies,
  the check constraints refuse a period ending before it starts and a month of 13, a role cannot be
  deleted while projects reference it, translations are stored resolved one row per language, and
  **reseeding unchanged content changes nothing**.
- The parity check is now a test as well as a script, so the two content sources are compared on
  every push instead of when someone remembers.
- `RequiresDockerFact` skips the container tests where there is no Docker rather than failing the
  suite. A suite that fails for environmental reasons is a suite people stop running.

#### Added — frontend
- `pathFor` and `translateUrl`, the two functions that already produced two shipped bugs. Every case
  now asserts the path is absolute.
- `ContentService`: content before any request, the snapshot kept when the API errors, the
  cached-content flag not raised before anything was tried, and a response that parses but is not
  content being rejected — a captive portal returning valid JSON would otherwise blank the site.
- `SeoService`: reciprocal `hreflang` pointing at the translated page, and tags replaced rather than
  appended across navigations, since appending would leave a page declaring four canonicals.
- `LocaleService` against a real router, checking the four things that must never disagree about the
  current language: the URL, Transloco, the content and `<html lang>`.
- The language switcher rendered, asserting every link is absolute and that switching from
  `/es/proyectos/linkvest` produces `/en/projects/linkvest`.

#### Changed
- `collect-facts.mjs` counts both suites, so the engineering page reports 151 rather than only the
  backend's 89. The testing section now describes what is actually covered.

### Phase 8 — CI (2026-08-24)

Every check in this pipeline already existed as a script somebody had to remember to run. Moving
them here is the point: they now run whether or not anyone remembers.

#### Added
- `.github/workflows/ci.yml` with six jobs:
  - **content** — regenerates the engineering figures and fails if the committed ones are stale, so
    the page cannot claim more tests than exist; runs the content validator.
  - **security** — the secret scan, vulnerable NuGet packages, and `npm audit` on production
    dependencies.
  - **api** — build with warnings as errors (which makes "it compiles" a real gate, so no separate
    lint step is needed) and the test suite, with results uploaded.
  - **web** — `npm ci`, prerender, then assertions that the generated HTML actually contains the
    content, the reciprocal `hreflang` and the JSON-LD. A prerendered page that renders an empty root
    element passes every build check and fails at the only thing prerendering is for.
  - **contract** — starts both content sources against a PostgreSQL service and runs the parity
    check, then verifies the committed snapshot still matches what the API serves.
  - **images** — builds the CVs, builds both images, brings the stack up and smoke-tests it through
    nginx, including **stopping the API** to confirm the site still serves its content.
- `tools/security/scan.mjs` — private keys, cloud credentials, tokens, passwords in connection
  strings, a Colombian phone number, and files that must never be tracked. Dependency-free on
  purpose: a security check that pulls a third-party action to run is new supply-chain surface for
  the thing it protects. Verified by planting a fake AWS key and password and watching it fail.
- `SKIP_CV=1` on the snapshot builder, for the CI job that has no browser to render a PDF with.

#### Notes
- The NuGet vulnerability gate is written as an `if`. `grep -q X && (exit 1) || true` always
  succeeds — a gate that reports green while finding vulnerabilities is worse than no gate.
- `npm audit` runs with `--omit=dev`. A dev-only advisory never reaches a user, and failing on one
  turns the job into noise people learn to skip, which is how the real ones get missed.
- Images are built but not pushed. A registry belongs with a deployment target, and that decision is
  deliberately deferred to phase 11.

### Phase 7 — Docker (2026-08-24)

#### Added
- `infra/docker/api.Dockerfile` — .NET 10 multi-stage, non-root Alpine runtime with no shell, a
  read-only filesystem and a tmpfs for `/tmp`. The repository layout is reproduced inside the image
  rather than flattened, because the API links `content/*.json` by relative path.
- `infra/docker/web.Dockerfile` — Angular prerender then nginx. No Node reaches the runtime image.
  Fails the build when the generated CVs are missing rather than shipping a dead download button.
- `infra/docker/nginx.conf` and `security-headers.conf` — static routing, API proxy, per-type
  caching, CSP and the rest.
- `docker-compose.yml` now brings up db, api and web. The API is **not** published to the host: it is
  reachable only through nginx, which is how it will be deployed, so the browser sees one origin and
  the API keeps an empty CORS allowed-origins list.
- `.dockerignore`, and explicit `404` routes so each locale prerenders a real error document.

#### Fixed
- **Security headers were silently dropped on every HTML response.** nginx replaces inherited
  `add_header` directives instead of merging them, so the block that set `Cache-Control` on `.html`
  wiped the entire Content-Security-Policy. `try_files` is what made it reachable: the internal
  redirect from `/en/` to `/en/index.html` re-evaluates locations. Found by reading the response,
  not the config. The headers now live in a snippet included at server level and inside every
  location that sets a header of its own.
- The image build failed on EF's generated migrations because the root `.editorconfig`, which marks
  them as generated code, was never copied into the build context.
- The 404 page was prerendered with the home page's title and was indexable. It now carries its own
  localised title and `noindex`.

#### Verified
- `docker compose up --build` → three healthy containers.
- Every route in both languages, the CV download, the API through the proxy, and a real **404** for
  an unknown URL — no SPA catch-all answering 200 for links that do not exist.
- Security headers and immutable caching on hashed assets; `no-cache` on HTML.
- **With the API stopped the site still serves complete content**; only `/api` fails, and the
  frontend falls back to its embedded snapshot.

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
