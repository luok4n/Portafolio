# Content review

Every difference between the CV as extracted (`cv-source.md`) and the content published in
`*.json`, with the reason and who authorised it. Nothing changes here without an entry.

- **Reviewed:** 2026-08-24
- **Authorised by:** Sebastián Vélez Ramírez (the author), in conversation on 2026-08-24

## How the CV was read

The PDF had no text-extraction tooling available on the machine, so the content streams were
inflated and decoded directly, including the embedded subset fonts' `ToUnicode` CMaps — without
which roughly a third of the bullet text (everything set in the subsetted font) was silently
missing. Worth stating because a partial extraction would have produced a portfolio missing
content with no visible sign that anything was wrong.

## Inconsistencies found, and how each was resolved

| # | Finding | Resolution |
|---|---|---|
| 1 | The CV had no entry between Woldev (ends Jan 2019) and MVM (starts Oct 2019) | Not a gap: **Universidad Tecnológica de Pereira, Jan–Oct 2019** was missing from the PDF. The author confirmed the role and does not know why it was dropped. Added. |
| 2 | "7+ years of experience" | Recalculated. With the UTP role, unique months worked total **102** as of Aug 2026 → **8+ years**. Computed at build time, never hardcoded. |
| 3 | LendingFront (Jan–Dec 2022) overlaps AES Chivor (Mar–Dec 2022) | Correct: the AES engagement was freelance and ran in parallel. Marked with `parallelWith` so the timeline shows it as deliberate rather than as a data error. |
| 4 | Company written as "Adagetech"; LinkedIn shows "Adage Technologies LATAM" | Author confirmed **Adagetech S.A.S.** |
| 5 | *Argos One* and *Linkvest* listed under Featured Projects with no employer | Both belong to **Adagetech**. Linked via `experienceId`. |
| 6 | Graduation year: CV says 2018, public LinkedIn shows other ranges | Author confirmed **2018**. |
| 7 | Adagetech ends Jul 2026 | Author is **actively looking**. Recorded as `availability: open-to-work`; drives the hero call to action. |
| 8 | Java and Spring Boot absent from Technical Skills | Added — they are the UTP stack. Java under Languages, Spring Boot under Backend. |
| 9 | Summary lists energy, fintech, real estate and education as sectors | Added **public health**, the sector of the RIAS programme. |
| 10 | "MOA" written as a product name with no expansion | Left as-is. No public documentation of the acronym exists; describing it by function is accurate, guessing an expansion would not be. |

## Content added that is not in the CV

| Item | Basis |
|---|---|
| UTP role, Jan–Oct 2019, Software Developer, Java + Spring Boot + Angular, RIAS programme | Author, 2026-08-24. Job title chosen by the author from the options offered. |
| **UTP responsibility bullets** | ⚠️ **Drafted, pending approval.** Written from the author's description (Java/Spring Boot backend, Angular frontend, RIAS) and flagged `highlightsStatus: draft-pending-author-approval` in `experience.en.json`. They must be approved or rewritten before publication. |
| LendingFront teams: development, innovation, optimisation | Author, 2026-08-24. |
| Linkvest contribution: monthly and quarterly investment reporting | Author, 2026-08-24. |
| MOA: Excel report generation | Author, 2026-08-24. |
| Argos ONE: support and development for the United States, Colombia and other countries | Author, 2026-08-24; consistent with the separately published ArgosONE USA application. |
| MVM client: Comfama | Author, 2026-08-24. |
| Woldev: Gobernación de Risaralda institutional website, legacy PHP | Author, 2026-08-24. |
| GitHub profile link | The author's own account, added because a portfolio without one is odd. Not professional history, so it needs no CV backing — but it is an addition and is recorded as such. |
| Client and sector descriptions | Public sources only, each with a URL and check date in `clients-research.md`. |

## Deliberately not written

- **Teach at Home** and the **Gobernación de Risaralda** website have no public source. Described by
  function and client only, with `verified: false`.
- **SimuDat Salud Risaralda** was considered as a candidate for the Woldev engagement because it is
  the Gobernación's best-documented technology programme of that period and touches RIAS. The author
  ruled it out. Recorded so the hypothesis is not revived later.
- No metric appears that is not already in the CV (the 30% query improvement and the 15% delivery
  time reduction are quoted verbatim; nothing new was invented).

## Still open

1. **UTP responsibility bullets** — need the author's approval or corrections.
2. **Comfama contribution** — what the author actually did there; `contribution` is `null` until then.
3. **Spanish translation** — no `*.es.json` exists yet. The CV has no Spanish source of truth, so
   every translated string needs explicit approval before it is committed (ADR-0001).

## Regenerating the CV

The CV is generated from this content, not edited by hand, so it cannot drift from the site:

```bash
node tools/cv/build-cv.mjs
```

Outputs to `dist/cv/` (untracked):

- `Sebastian_Velez_CV_public.pdf` — no phone number. This is the one published on the site.
- `Sebastian_Velez_CV_full.pdf` — includes the phone, read from `content/private/contact.local.json`,
  which is untracked. On a fresh clone this variant is simply skipped.
