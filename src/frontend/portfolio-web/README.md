# Portfolio web

Angular 22 frontend, prerendered to static HTML. Bilingual English/Spanish.

Design and behaviour: [functional-design.md](../../../docs/functional-design.md).
Rendering decision: [ADR-0002](../../../docs/adr/0002-frontend-rendering.md).

## Build

```bash
npm run build:full
```

That runs three generators and then Angular:

1. `collect-facts.mjs` — measures the repository for the engineering section's numbers.
2. `build-cv.mjs` — builds the downloadable CV in both languages.
3. `build-snapshot.mjs` — fetches the content from the API into `src/content-snapshot/` and copies
   the redacted CVs into `public/cv/`.

Step 3 needs the API running (`dotnet run --project Portfolio.Api --urls http://localhost:5080`).
That is deliberate: re-reading `content/` here would reimplement the base-locale/translation merge a
third time, and a build that silently used a second implementation would be worse than one that
fails.

```bash
npm run build
```

Builds without regenerating, using the committed snapshot.

```bash
npm start
```

Dev server on 4200, proxying `/api` to `localhost:5080`.

## Why the content is imported rather than fetched

`src/content-snapshot/*.json` is imported by TypeScript and compiled into the bundle. Prerendering is
therefore deterministic and needs no network: every route becomes complete HTML at build time.

In the browser the app then revalidates against the API and swaps in anything newer. If the API is
unreachable, cold-starting or slow, the page stays complete and a discreet notice says the content is
cached. **There is no loading state and no error state for content anywhere in this app** — the
content is already in the HTML, so the API can only improve it.

The snapshot is committed even though it is generated: it is a build input, so a fresh clone must be
able to build, and being JSON it shows content changes as a readable diff. The CV PDFs are not
committed — a binary a command reproduces does not belong in the history.

## Routes

Generated from one table in `src/app/core/locale.ts`, which the router, the language switcher, the
prerender route generator and `hreflang` all read.

| Route | English | Spanish |
|---|---|---|
| Home | `/en` | `/es` |
| Project | `/en/projects/{slug}` | `/es/proyectos/{slug}` |
| Engineering | `/en/engineering` | `/es/ingenieria` |
| Entry point | `/` → redirects, `noindex` | |

The path segment is translated; the project slug is not, because it is a proper noun.

The language switcher keeps the route and the anchor: from `/es/proyectos/slang` it goes to
`/en/projects/slang`, not to the home page.

## i18n

- **UI strings** — Transloco, `src/i18n/{en,es}.json`, imported rather than fetched so they resolve
  during prerendering and need no request on first paint.
- **Content** — translated server-side and delivered already resolved.

## Structure

```text
src/app/
├── core/        locale table, content service, engineering content, SEO, formatting
├── layout/      header with the language switcher, footer, cached-content notice
├── sections/    the home page's sections
├── pages/       home, project detail, engineering, 404, locale redirect
└── diagrams/    hand-written inline SVG — no diagramming library
```

Diagrams are inline SVG using theme tokens: they prerender, work without JavaScript, follow light
and dark, and do not add a dependency to draw four boxes.
