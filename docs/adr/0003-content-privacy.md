# ADR-0003 — What personal and client information is published

- **Status:** Accepted
- **Date:** 2026-08-24

## Context

This repository is public and the site will be indexed. The source material — a CV and the author's
recollection of client work — contains information that ranges from "meant to be public" (job
titles) to "should never leave the laptop" (a personal phone number), with client project details
somewhere in between.

Two distinct risks:

- **Personal data.** A phone number on an indexed page is harvested within days by scrapers and
  automated recruiters, and cannot be un-published once mirrored.
- **Client confidentiality.** Describing employers' clients from memory risks publishing internal
  details covered by contract or NDA, which is a professional liability rather than a technical one.

## Decision

### Personal data

| Item | Published on the site | Committed to the repo |
|---|---|---|
| Name, headline, city/region | Yes | Yes |
| Professional email | Yes | Yes |
| LinkedIn profile | Yes | Yes |
| **Phone number** | **No** | **No** |
| Exact home address | No | No |
| Original CV PDF | No | **No** — listed in `.gitignore` |
| Redacted CV PDF (no phone) | Yes, as a download | Yes, as a frontend asset |

The original `Sebastian_Velez_CV_Updated.pdf` stays on the author's machine only. A redacted copy
is generated for the "Download CV" action.

### Client and project information

1. Only **publicly verifiable** information about clients is published: what the company does, its
   sector and market, sourced from its own website, official documentation or press coverage.
2. Every enriched client fact carries a source URL in `content/`, so any claim can be traced.
3. **Never published:** internal architecture, credentials, incident details, unreleased roadmaps,
   named individuals, contract terms, or any figure not already in the CV or public record.
4. Metrics already stated in the CV (for example "reduced response times by 30%") may be repeated
   verbatim; new metrics are not invented.
5. Where a client relationship itself may be sensitive, the project is described by domain
   ("a wholesale energy trading platform") rather than by client name, subject to author approval.

### Repository hygiene

- No secrets, connection strings, tokens or cloud keys in Git, in any branch, at any point in
  history — enforced by `.gitignore` and by secret scanning in CI (phase 8).
- Local overrides go in `appsettings.Local.json` / `.env`, both ignored.

## Consequences

**Positive**
- Contactable without exposing a phone number to scrapers.
- Client content is defensible: every claim traces to a public source or to the CV.
- Traceability makes the phase 1 content review meaningful rather than ceremonial.

**Negative**
- Sourcing every client fact is slower than writing from memory.
- The redacted CV is a second artefact that must be regenerated whenever the CV changes.
- Some genuinely impressive work will be described more vaguely than the author might like. That
  is the correct trade-off.
