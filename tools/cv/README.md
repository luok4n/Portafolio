# CV builder

Generates the CV from `content/` as HTML, then converts it to PDF with headless Chrome or Edge.

```bash
node tools/cv/build-cv.mjs
```

```bash
node tools/cv/build-cv.mjs --locale es
```

```bash
node tools/cv/build-cv.mjs --html-only
```

## Why generate it

The CV and the portfolio state the same facts. Keeping the CV as a hand-edited binary guarantees
they drift apart, and makes "what changed since the last version" unanswerable. Generating it from
the same content that seeds the site means a correction is made once, and regenerating is a command
rather than an afternoon in a word processor.

## Output

Written to `dist/cv/` (untracked), two locales times two variants:

| File | Phone | Purpose |
|---|---|---|
| `Sebastian_Velez_CV_EN_public.pdf` | No | CV download on the English site |
| `Sebastian_Velez_CV_ES_public.pdf` | No | CV download on the Spanish site |
| `Sebastian_Velez_CV_EN_full.pdf` | Yes | Direct applications |
| `Sebastian_Velez_CV_ES_full.pdf` | Yes | Direct applications |

The phone number lives in `content/private/contact.local.json`, which is untracked per
[ADR-0003](../../docs/adr/0003-content-privacy.md). Without that file only the public variants are
produced, so a fresh clone or a CI run never fails and never leaks the number.

## Fitting two pages

The builder renders, counts the `/Type /Page` objects in the resulting PDF, and shrinks the
typographic scale until the CV fits two pages — currently 1.00 for English and 0.97 for Spanish,
which runs about 15% longer for the same content.

Fitting by measurement rather than by eye means a new bullet cannot silently push the CV onto a
third page, and neither locale needs hand-tuning when content changes. If it still does not fit at
minimum scale, the build says so instead of quietly producing three pages.

## Years of experience

Never hardcoded. `build-cv.mjs` expands every role into the set of months it covers and divides the
size of that set by twelve, so the freelance period that overlaps a full-time role is counted once.
The same rule will drive the number shown on the site.

## Locale resolution

`*.en.json` is the base locale and owns ids, dates, technologies and sources. `*.es.json` carries
only translatable fields, matched by id, and is merged over the base at render time. Adding a third
language means adding one translation file and one entry in `LABELS`.

## Chrome discovery

Chrome and Edge are probed at their usual Windows, Linux and macOS locations. Override with
`CHROME_PATH` if neither is found.
