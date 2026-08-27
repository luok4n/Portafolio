# Infrastructure

```text
docker/
├── api.Dockerfile           .NET 10 multi-stage → non-root Alpine runtime
├── web.Dockerfile           Angular prerender → nginx, no Node in the runtime image
├── nginx.conf               static routing, API proxy, caching
└── security-headers.conf    included per location — see the note below
scripts/
```

## Run the whole system

```bash
docker compose up --build
```

http://localhost:8080. The web container serves the prerendered site and proxies `/api` to the API,
so the browser sees one origin and CORS never enters the picture — the API ships with an empty
allowed-origins list in production, and that is not an oversight.

For the fast edit-run loop, bring up only the database and run the API and frontend from the host:

```bash
docker compose up -d db
```

## Generated assets

`public/cv/` and `public/og/` are tracked, so the image builds from a clean clone. Regenerate them
after a content change:

```bash
cd src/frontend/portfolio-web && npm run content
```

The Dockerfile still checks they exist and fails the build if they are missing, rather than shipping
a site whose "Download CV" button 404s — the kind of defect nobody notices until a recruiter clicks
it.

## The nginx header trap

nginx does **not** merge `add_header` across levels. A `location` block that declares any header of
its own silently drops every header inherited from the server block. No warning, no error, just a
response that quietly lost its Content-Security-Policy.

`try_files` makes it worse: the internal redirect from `/en/` to `/en/index.html` re-evaluates
locations, so the `.html` block ends up handling a request whose URI does not look like HTML.

That is exactly what happened here, and it was found by reading the response rather than the config:

```bash
curl -sD - -o /dev/null http://localhost:8080/en/
```

The headers therefore live in `security-headers.conf`, included at server level **and** inside every
location that sets a header.

## Container posture

| | |
|---|---|
| API user | non-root, no shell, no home directory |
| API filesystem | read-only, with a tmpfs for `/tmp` |
| Web user | `nginx`, listening on 8080 because an unprivileged process cannot bind 80 |
| Both | `no-new-privileges` |
| API ports | not published to the host — reachable only through the web container |

Liveness checks deliberately touch nothing but the process. A content or database problem must not
make an orchestrator restart a service that is running perfectly well; that is what readiness is for.

## Verified

- `docker compose up --build` brings all three containers to healthy.
- Every route, both languages, the CV download and the API through the proxy answer correctly.
- An unknown URL returns a real **404**, not a 200 with the home page. There is no SPA catch-all
  rewrite: answering 200 for URLs that do not exist hides broken links from crawlers and from us.
- With the API stopped, the site still serves complete content — only `/api` fails, and the frontend
  falls back to its embedded snapshot.
