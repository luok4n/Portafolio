# Security

What this project defends against, how, and — just as important — what it does not.

A portfolio is a low-value target with a public codebase. That combination decides the shape of
everything below: the realistic threats are automated scanners, scrapers, and the author committing
something he should not. There is no user data to steal, no session to hijack, and no money to move.
Claiming otherwise would be theatre, and a security section full of theatre is worse than a short
honest one.

Last reviewed: **2026-08-24**.

---

## Secrets

Nothing sensitive is in Git, in any branch, at any point in history.

| | |
|---|---|
| Local development credentials | Committed on purpose, and deliberately boring: `portfolio/portfolio` against a container on localhost. Treating them as secrets would train everyone to ignore the tool that flags real ones. |
| The author's phone number | Never committed. Lives in `content/private/contact.local.json`, which is untracked, and the CV that contains it is generated locally and never published. |
| Anything else | There is nothing else yet. Deployment credentials arrive in phase 13 and will live in the platform's secret store, never in the repository. |

Enforced by [`tools/security/scan.mjs`](../tools/security/scan.mjs), which runs in CI on every push
and fails on private keys, cloud access keys, tokens, passwords in connection strings, a Colombian
phone number, and paths that must never be tracked. It is dependency-free on purpose: a security
check that pulls a third-party action to run is new supply-chain surface for the thing it protects.

It scans the working tree, not history. That is a different job with different tooling, and saying
this covers it would be worse than not having it.

A second net: an integration test asserts that **no API response ever contains a phone number**, so
a future content change cannot reintroduce it through a field nobody thought about.

## What is published

Decided in [ADR-0003](adr/0003-content-privacy.md), not left to whoever writes the next section.

- Name, headline, city, professional email and public profiles: yes.
- Phone number, exact address: no, ever.
- Client information: only what a public source already says, with the URL and the date it was
  checked recorded next to it. Nothing about internal architecture, credentials, incidents,
  unreleased work, contract terms or named individuals.
- The CV download is the redacted variant, and it is the only one tracked. The one with the phone
  number is built locally, stays in `dist/`, is excluded from the Docker build context, and the
  secret scan fails on any tracked file named `_full.pdf`. Four independent things would have to go
  wrong for it to be published.

## Transport

HTTPS is **not yet configured** — there is no deployment yet. It arrives in phase 13 together with
the domain, and until then this line stays as it is rather than describing an intention as a
control.

What exists today: nginx sets `Strict-Transport-Security` only once there is TLS to enforce, because
sending HSTS over plain HTTP is meaningless and sending it from a development container can lock a
developer's browser out of `localhost` for a year.

## Headers

Set by nginx in [`security-headers.conf`](../infra/docker/security-headers.conf), included at server
level **and inside every location that sets a header of its own** — nginx replaces inherited
`add_header` directives rather than merging them, which had already cost this project its entire
Content-Security-Policy without a single warning.

| Header | Value | Why this value |
|---|---|---|
| `Content-Security-Policy` | `default-src 'self'`, `object-src 'none'`, `frame-ancestors 'none'` | The site loads nothing from anywhere else — no CDN, no analytics, no external fonts — so the policy can be strict enough to be worth having. `'unsafe-inline'` remains for the hydration and JSON-LD scripts Angular inlines into each prerendered page; removing it means per-build nonces, which is phase 10 work if it is worth doing at all. |
| `X-Content-Type-Options` | `nosniff` | |
| `X-Frame-Options` | `DENY` | Belt and braces alongside `frame-ancestors`. |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | |
| `Permissions-Policy` | camera, microphone, geolocation and payment all denied | The site needs none of them. |
| `Cross-Origin-Opener-Policy` | `same-origin` | |
| `server_tokens` | `off` | The server's version is not information a visitor needs. |

Verified by reading the actual response, not the config:

```bash
curl -sD - -o /dev/null http://localhost:8080/en/
```

## CORS

The allowed-origins list is **empty in production**, and that is not an oversight. The browser only
ever reaches the API through nginx on the same origin, so there is no cross-origin request to
permit. Development allows `localhost:4200` for the Angular dev server, and only GET, only the
headers actually used.

An empty list means *no* cross-origin access rather than *any* — the safe default is the restrictive
one.

## Input

The API is read-only: there is no write endpoint, no form, no upload, and no user-supplied content
rendered anywhere.

What input exists is still treated as untrusted:

- **Language** — matched against a closed set. An unsupported or malformed value falls back to
  English rather than failing, because a broken header from some client is not a reason to refuse a
  public page.
- **Correlation id** — accepted from the caller, but length-capped and restricted to characters that
  cannot forge a log line or a response header. It ends up in both. Covered by tests that feed it
  newlines, a script tag and a 4 KB string.
- **Project id** — used only as a lookup key; an unknown one produces a 404 problem document.
- **Errors** — only exceptions this application defines contribute their message to a response.
  Anything unexpected returns a generic title, because a stack trace or a file path is not the
  public's business.

## Rate limiting

A fixed window per caller: 300 requests per minute by default, partitioned by the forwarded address.

This is not protecting a database — the API is read-only and its answers are cached. It exists so
that one badly written scraper cannot spend the compute budget of whatever this ends up deployed on.
The limit is far above what reading the site produces: the whole page is one request.

Health checks and metrics are **never** limited. Limiting them would let a burst of traffic convince
an orchestrator that the service is down and restart a process that is working fine.

Rejections return `429` with `Retry-After`. Without it a client has no way to back off correctly, so
it retries immediately and makes the situation worse.

## Containers

| | |
|---|---|
| API user | non-root, no shell, no home directory |
| API filesystem | read-only, with a tmpfs for `/tmp` |
| Web user | `nginx`, listening on 8080 because an unprivileged process cannot bind 80 |
| Both | `no-new-privileges` |
| API network exposure | not published to the host; reachable only through the web container |
| Base images | Alpine variants of the official .NET and nginx images, pinned to a minor version |

Nothing is given up by locking the API down: it is stateless and never writes.

## Dependencies

- `dotnet list package --vulnerable --include-transitive` fails CI on a high or critical advisory.
  Written as an `if`: `grep -q X && (exit 1) || true` always succeeds, which is a gate that reports
  green while finding vulnerabilities.
- `npm audit --audit-level=high --omit=dev`. Production dependencies only — a dev-only advisory
  never reaches a user, and failing on one turns the job into noise people learn to skip, which is
  how the real ones get missed.
- The dependency list is short by policy. Nothing is added that cannot justify what it costs, which
  is a security property as much as an architectural one.

Base images are pinned to a minor version and updated deliberately. There is no bot opening
dependency pull requests yet; that is worth adding once the project is deployed and the update is
not just a rebuild.

## Least privilege

- The API has one database user, and it is the only account that exists.
- The static site has no credentials at all.
- Nothing in the running system can write to the repository or to the content files.

## What this does not do

Stated plainly, because a security document that only lists wins is not a security document.

- **No authentication or authorisation.** There is nothing to protect: every endpoint serves content
  that is already public. Adding auth to a read-only public API would be complexity with no threat
  behind it.
- **No WAF, no DDoS protection.** Out of proportion for a personal site, and mostly a property of
  whichever platform it is deployed to. Revisited in phase 11.
- **No secret scanning of Git history.** The working tree is scanned on every push; history is a
  separate exercise with separate tooling.
- **No CSP nonces.** `'unsafe-inline'` remains for Angular's inlined hydration script. It is a real
  weakening of the policy on a site with no user input to inject through.
- **No audit logging.** Requests are logged with method, route, status, duration and correlation id,
  which is observability rather than an audit trail. There is no privileged action to audit.
- **No HTTPS yet.** See above; phase 13.

## If something is found

The repository is public and so is the author's email. A report by email is the right channel; there
is no bounty, and there is no user data at risk, but a real finding will be fixed and recorded here.
