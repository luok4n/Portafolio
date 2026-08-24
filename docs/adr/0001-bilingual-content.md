# ADR-0001 — Bilingual content (English default, Spanish parity)

- **Status:** Accepted
- **Date:** 2026-08-24
- **Supersedes:** §36 of the original development plan, which listed multi-language support as
  out of scope for the MVP.

## Context

The portfolio targets two audiences that do not overlap: international recruiters and engineering
managers hiring for remote/Senior .NET roles (English), and the Colombian/LATAM market (Spanish).
Serving only one of them halves the reach of the site.

The source CV exists only in English. Spanish content is therefore a **translation of verified
material**, not new information — the no-invented-content rule of the development plan still applies
to both languages.

Adding a second language after the data model and the API exist is expensive: it changes entity
shapes, endpoint contracts, routing and SEO metadata. It has to be decided now, not during polish.

## Decision

1. **English is the default language.** The canonical routes are `/en/...`; `/` redirects to `/en`.
   Spanish is served from `/es/...` with full content parity — not a reduced version.
2. **No browser-based auto-redirect.** Language is chosen by URL and by an explicit switcher that
   persists the choice. Auto-detection would fight prerendering and produce unstable canonical URLs.
3. **Two distinct kinds of text, handled differently:**
   - *UI strings* (labels, buttons, section titles) live in translation files consumed by
     **Transloco** at runtime, so switching language does not require a rebuild or a round trip.
   - *Portfolio content* (summary, experience bullets, project descriptions) lives in the database
     with one translated row per locale, and is served by the API.
4. **The API is locale-aware.** Content endpoints accept an explicit `?lang=en|es` and fall back to
   the `Accept-Language` header, then to English. Every response states which locale it resolved to.
5. **The content model stores translations as rows, not as columns.** A `*_Translation` table keyed
   by `(EntityId, LanguageCode)` — adding a third language later becomes a data change instead of a
   schema migration.
6. **SEO is bilingual from the start:** per-locale `<title>`/`<meta>`, reciprocal `hreflang`
   annotations between `/en` and `/es`, a self-referencing canonical per locale, and both locales
   listed in the sitemap.

## Consequences

**Positive**
- Reach both audiences with one deployment.
- The translation table is itself an interview talking point (i18n data modelling, fallback chains).
- Language switching is instant; no server round trip for UI chrome.

**Negative**
- Every content change is now two changes. The content review step in phase 1 must sign off both
  languages before seeding.
- Prerendering doubles the number of generated routes.
- Tests must cover the locale fallback chain (`?lang` → `Accept-Language` → `en`), including
  unknown and malformed locale codes.

**Follow-up**
- Language switching must preserve the current route and scroll position.
- The Spanish translation of the CV-derived content requires explicit author approval before it is
  committed, since the CV itself has no Spanish source of truth.
