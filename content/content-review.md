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
| UTP responsibility bullets | Drafted from the author's description and **approved by the author on 2026-08-24**. |
| Comfama account: Slang alliance, internal website management, teacher grading platform | Author, 2026-08-24. |
| MVM bullet naming Comfama | Added to the CV: the CV named CHIVOR XM and SLANG but not Comfama, which was a third of the MVM work. |
| LendingFront teams: development, innovation, optimisation | Author, 2026-08-24. |
| Linkvest contribution: monthly and quarterly investment reporting | Author, 2026-08-24. |
| MOA: Excel report generation | Author, 2026-08-24. |
| Argos ONE: support and development for the United States, Colombia and other countries | Author, 2026-08-24; consistent with the separately published ArgosONE USA application. |
| MVM client: Comfama | Author, 2026-08-24. |
| Woldev: Gobernación de Risaralda institutional website, legacy PHP | Author, 2026-08-24. |
| GitHub profile link | The author's own account, added because a portfolio without one is odd. Not professional history, so it needs no CV backing — but it is an addition and is recorded as such. |
| Client and sector descriptions | Public sources only, each with a URL and check date in `clients-research.md`. |

## Deliberately not written

- **Teach at Home** has no public source. Described by function and client only, with
  `publiclySourced: false`.
- **SimuDat Salud Risaralda** was considered as a candidate for the Woldev engagement because it is
  the Gobernación's best-documented technology programme of that period and touches RIAS. The author
  ruled it out. Recorded so the hypothesis is not revived later.
- No metric appears that is not already in the CV (the 30% query improvement and the 15% delivery
  time reduction are quoted verbatim; nothing new was invented).

## Slang: an identity that had to be checked

When the author said Slang was one of the projects under the Comfama account, the description
already researched — the Colombian EdTech startup with the MIT origin, the US$14M Series A and the
2022 World Economic Forum recognition — stopped being safe to publish. "SLANG" could equally have
been an internal project name for a Comfama learning platform with no connection to the startup,
and attaching another company's achievements to it would have been a factual error on a public page.

Flagged as `identityStatus: pending-disambiguation` and resolved by the author on 2026-08-24: it is
the startup. Slang and Comfama ran an alliance giving the fund's affiliates access to the English
courses with member benefits, so the research stands and the two entries describe one engagement
from two sides.

## Approvals

| Item | Status |
|---|---|
| UTP responsibility bullets | ✅ Approved by the author, 2026-08-24 |
| Full Spanish translation (`profile`, `experience`, `projects`, `education`) | ✅ Approved by the author, 2026-08-24 |
| Slang identity | ✅ Resolved by the author, 2026-08-24 |

## The flag was called `verified`, and that was the wrong name

It never meant "this work happened" — it meant "a reader can check the client and the domain against
a public source". Called `verified`, a `false` read as doubt about work the author actually did, on
his own portfolio. Renamed to **`publiclySourced`**, which is what it measures, on 2026-08-24.

The rename made the remaining question a real one instead of a semantic one: which projects can a
reader actually check?

| Project | `publiclySourced` | Why |
|---|---|---|
| **AES Chivor MOA** | ✅ true | The client, the Chivor plant and the Colombian wholesale market are documented by AES Colombia and XM. Only the internal product name is not public, and the description does not claim otherwise. |
| **Gobernación de Risaralda** | ✅ true | `risaralda.gov.co` is the department's official site — the client and the artefact are both public record. Source added 2026-08-24. |
| **Teach at Home** | ❌ false | Searched again on 2026-08-24; no public trace. Setting it true would assert a citation that does not exist, and the page shows no sources block either way — the flag would change nothing except break the validator. |

Nine of eleven projects now carry a source. If a public link for Teach at Home ever turns up — a
company page, an app listing, a press note — adding it flips the flag honestly.

## Regenerating the CV

The CV is generated from this content, not edited by hand, so it cannot drift from the site:

```bash
node tools/cv/build-cv.mjs
```

Outputs four files to `dist/cv/` (untracked) — two locales times two variants:

- `Sebastian_Velez_CV_{EN,ES}_public.pdf` — no phone number. Published on the site.
- `Sebastian_Velez_CV_{EN,ES}_full.pdf` — includes the phone, read from
  `content/private/contact.local.json`, which is untracked. On a fresh clone these are skipped.
