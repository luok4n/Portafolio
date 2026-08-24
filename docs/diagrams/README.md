# Diagrams

There are no image files here, and that is deliberate.

Diagrams live in two places, each next to the thing it describes:

- **[`../architecture.md`](../architecture.md)** — Mermaid, rendered by GitHub, written inline with
  the prose it illustrates. An exported PNG next to a paragraph is a diagram that will disagree with
  that paragraph within two commits, and nothing will notice.
- **[`../../src/frontend/portfolio-web/src/app/diagrams/`](../../src/frontend/portfolio-web/src/app/diagrams/)**
  — hand-written inline SVG components for the site's engineering page. They prerender, work without
  JavaScript, follow the theme tokens, and take their labels from the translated content, so the
  Spanish page has no English text inside its diagrams.

A binary a command cannot reproduce does not belong in this repository. The same rule keeps the
generated CV and the social preview images out of Git.
