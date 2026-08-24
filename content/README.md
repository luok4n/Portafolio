# Content

Structured, reviewed portfolio content. This directory is the single source for the database seed —
no professional information is hardcoded in templates or in C# files.

Populated in phase 1. Planned files:

```text
content/
├── cv-source.md              # Text extracted from the CV, unaltered
├── profile.en.json
├── profile.es.json
├── experience.en.json
├── experience.es.json
├── projects.en.json
├── projects.es.json
├── skills.json               # Locale-independent identifiers + localised labels
├── education.en.json
├── education.es.json
├── social-links.json
└── content-review.md         # Detected inconsistencies and how each was resolved
```

## Rules

1. **Nothing is invented.** Every company, role, date, technology, project, metric and achievement
   comes from the CV or from a public source cited inline.
2. **Every enriched fact carries a source.** Client and project descriptions gathered from public
   sources include a `sources` array with URLs and the date they were checked.
3. **Spanish is a translation, not a rewrite.** The `.es.json` files mirror the `.en.json`
   structure exactly, key for key, and are approved by the author before being committed.
4. **No confidential material.** See [ADR-0003](../docs/adr/0003-content-privacy.md) for what may
   and may not be published about clients.
