# CV builder

Generates the CV from `content/` as HTML, then converts it to PDF with headless Chrome or Edge.

```bash
node tools/cv/build-cv.mjs
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

Written to `dist/cv/` (untracked):

| File | Contains a phone number | Purpose |
|---|---|---|
| `Sebastian_Velez_CV_public.pdf` | No | Published on the site as the CV download |
| `Sebastian_Velez_CV_full.pdf` | Yes | Direct job applications only |

The phone number lives in `content/private/contact.local.json`, which is untracked per
[ADR-0003](../../docs/adr/0003-content-privacy.md). Without that file only the public variant is
produced, so a fresh clone or a CI run never fails and never leaks the number.

## Years of experience

Never hardcoded. `build-cv.mjs` expands every role into the set of months it covers and divides the
size of that set by twelve, so the freelance period that overlaps a full-time role is counted once.
The same rule will drive the number shown on the site.

## Chrome discovery

Chrome and Edge are probed at their usual Windows, Linux and macOS locations. Override with
`CHROME_PATH` if neither is found.
