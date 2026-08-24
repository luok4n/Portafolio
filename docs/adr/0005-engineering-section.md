# ADR-0005 — The engineering section ships with the application, and its numbers are generated

- **Status:** Accepted
- **Date:** 2026-08-24

## Context

The site gains a section explaining how it is built: why a frontend, an API and a database rather
than a static page, where the engineering weight actually sits, the architecture, the decisions, the
flows, the data model, the tests and the operational story.

The author is a backend engineer. A backend engineer's portfolio that is only a nicely styled CV
argues against itself — the interesting work is invisible. This section is where the work becomes
visible, which makes it the most load-bearing part of the site and the one most likely to be read
adversarially by an interviewer.

Two questions follow. Where does this content live? And how do we stop it lying?

## Decision

### It is application content, not portfolio content

Everything else on the site — experience, projects, skills — is professional history: it comes from
`content/`, gets seeded into PostgreSQL, and is served by the API. The engineering section is not
professional history. It describes **this codebase**, changes when the codebase changes, and is
reviewed in the same pull request as the change it describes.

So it ships with the frontend: authored in `content/engineering.{en,es}.json`, compiled into the
application at build time, no API round trip. A deployment cannot serve a description of an
architecture it is not running.

This is a deliberate exception to "all content comes from the API", and it is the only one.

### The numbers are generated from the repository

Any figure in this section — tests, tables, endpoints, ADRs, supported languages — is produced by
`tools/engineering/collect-facts.mjs` reading the actual artefacts: the test sources, the EF
migration, the OpenAPI document, `docs/adr/`. Never typed into the content file.

A section that claims 52 tests when there are 30 is worse than having no section, because it is
exactly the sort of claim a technical interviewer checks and it puts every other claim on the page
in doubt. Generating the numbers means the failure mode is a stale build, not a false statement.

### Diagrams are hand-written inline SVG

No diagramming library. The diagrams are four boxes and some arrows; they must prerender, work
without JavaScript, and follow the theme tokens. A runtime library would fail the first two and add
a dependency to draw a rectangle.

### It gets its own route

`/en/engineering` and `/es/ingenieria`, prerendered, alongside a summary on the home page. It has
enough content to be indexed on its own, and it is the link worth sending during an interview.

## Consequences

**Positive**
- The section cannot describe an architecture the deployment is not running.
- Its numbers cannot drift from reality without the build noticing.
- No API dependency, so it renders even in the fallback state of ADR-0002.
- It gives the backend work — the domain rule, the bilingual model, the storage seam, the parity
  check — a place to be seen, which is the entire point of the project.

**Negative**
- A second content path: most content is API-served, this one is compiled in. That inconsistency is
  real and is the price of the guarantee above.
- Changing the text requires a rebuild and deploy, unlike the rest of the content.
- `collect-facts.mjs` has to be kept honest as the repository grows; a fact it can no longer
  measure must be removed rather than hardcoded.
- The section must be updated when the architecture changes, or it becomes the most embarrassing
  page on the site. Treated as part of the definition of done for any architectural change.
