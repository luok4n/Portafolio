# ADR-0002 — Prerendered Angular (SSG) with a static content fallback

- **Status:** Accepted
- **Date:** 2026-08-24
- **Amends:** §14 and §15 of the original development plan, which assumed a client-rendered SPA
  built to static files.

## Context

A portfolio is judged partly by whether it can be found and shared. A pure SPA returns an empty
`<div id="root">` to crawlers and to the link-preview bots used by LinkedIn, WhatsApp and Slack —
exactly the channels through which a portfolio circulates. It also makes the bilingual `hreflang`
work of [ADR-0001](0001-bilingual-content.md) effectively invisible.

The content is read-only and changes a few times a year. There is no per-request personalisation,
no authenticated area, and no reason to render on every request.

A second concern: the hosting decision is deliberately deferred to phase 11 and may well land on a
free or low-cost tier where the API sleeps and cold-starts. A portfolio that shows spinners or an
error state while a recruiter looks at it is worse than one with slightly stale content.

## Decision

1. **Use `@angular/ssr` in prerender (SSG) mode.** Every route × locale is rendered to static HTML
   at build time. Production output is static files served by **nginx** — no Node process in
   production, so the Docker image and the hosting story stay simple.
2. **The build embeds a content snapshot.** At build time the content is fetched from the API (or
   read from `content/` when the API is not reachable) and shipped as a JSON asset.
3. **The API is the primary source at runtime; the snapshot is the fallback.** On load the app
   hydrates from prerendered HTML, then revalidates against the API. If the API is unreachable or
   slow, the prerendered content stands and the UI shows a discreet "showing cached content" note
   instead of an error.
4. **Rebuild on content change.** Content updates require a rebuild/redeploy. This is acceptable at
   the expected change frequency and is automated by CD.

## Consequences

**Positive**
- Correct SEO, correct social previews, correct `hreflang` per locale.
- Fast first paint; content is visible before any JavaScript executes.
- The site survives a backend outage or cold start — a realistic resilience story to discuss in an
  interview, not a contrived one.
- Cheapest possible frontend hosting: static files behind a CDN or nginx.

**Negative**
- Content edits are not live; they need a rebuild. If that ever becomes a real constraint, the
  escape hatch is switching the same `@angular/ssr` setup from prerender to on-demand SSR.
- Build time grows with routes × locales.
- Two content paths (API and snapshot) must be kept consistent, and the "stale content" state needs
  its own tests.

**Rejected alternatives**
- *Node SSR in production* — adds a container, a runtime and hosting cost for content that changes
  a handful of times per year.
- *Plain SPA* — the original plan's approach; rejected for the SEO and link-preview reasons above.
