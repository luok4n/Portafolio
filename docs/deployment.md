# Deployment

Step-by-step for the three platforms chosen in [ADR-0006](adr/0006-hosting.md):
**Cloudflare Pages + Google Cloud Run + Neon**, at $0/month plus the domain.

Nothing here has been executed yet. This is the plan to follow, written so it can be followed
without re-deriving anything.

- **Written:** 2026-08-27
- **Accounts needed:** Cloudflare, Google Cloud, Neon. All three have free tiers that cover this.
- **Only cost:** the domain, roughly $10–15 a year.

---

## Before anything: two things that will break a deploy

Both are known, both are quick, and both produce a site that looks fine and is quietly wrong if
skipped.

### 1. The generated assets are not in Git

`public/cv/` and `public/og/` are gitignored on purpose — a binary a command reproduces does not
belong in history. A build that clones the repository therefore produces a site with a **dead
"Download CV" button and no social preview image**, and nothing fails while that happens.

Two ways out. Pick one before configuring Cloudflare:

| | |
|---|---|
| **A — deploy from CI** *(recommended)* | GitHub Actions already builds the CVs and the preview images in the `images` job. Add a deploy step that runs the same generation and then `wrangler pages deploy`. Keeps the rule, and the deploy runs only after every check has passed. |
| **B — commit the artefacts** | Remove the two `.gitignore` entries and commit the four files. Simpler, and Cloudflare's git integration then works untouched. The cost is binaries in history and a rule with an exception in it. |

### 2. The site's own address is a placeholder

`src/frontend/portfolio-web/src/app/core/seo.service.ts` line 18:

```ts
const SITE_ORIGIN = 'https://sebastianvelez.dev';
```

That domain is not registered. It is where the canonical URLs, the `hreflang` links, the Open Graph
image URLs and — indirectly — the whole sitemap come from, since `build-sitemap.mjs` reads the
origin back out of the rendered canonical tag.

**Set it to the real domain before building the frontend**, or the deployed site will advertise
pages on a domain nobody owns. Say the word and this becomes a build-time environment variable
instead of a constant; it is a small change and it belongs before the first deploy rather than
after.

---

## Step 1 — Neon (PostgreSQL)

**Console:** https://console.neon.tech · **Pricing:** https://neon.com/pricing

1. Sign up at https://neon.com — GitHub sign-in works and is the fastest.
2. **Create a project.** Name it `portfolio`. Pick the region closest to where Cloud Run will run;
   keeping them in the same region is the difference between single-digit and three-digit
   milliseconds per query.
3. Postgres version: **17**, matching `docker-compose.yml` and the container the tests run against.
4. Once created, Neon shows a **connection string**. Take the **pooled** one — Cloud Run creates and
   destroys instances, and the pooler is what stops that exhausting connections.

It looks like this:

```text
postgresql://<user>:<password>@ep-<your-endpoint>-pooler.<region>.aws.neon.tech/portfolio?sslmode=require
```

5. **Convert it to the Npgsql format the API expects.** This is the step that silently fails
   otherwise: .NET does not read a `postgresql://` URL, and Neon refuses a connection without TLS.

```text
Host=ep-<your-endpoint>-pooler.<region>.aws.neon.tech;Database=portfolio;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
```

Keep that string somewhere safe for step 2. **Do not commit it** — `tools/security/scan.mjs` fails
the build if a password in a connection string reaches a tracked file, which is the point.

There is nothing to upload. The API creates the schema and loads the content itself on first start:
it runs the migration and then the seeder, which reads `content/` from inside the image.

---

## Step 2 — Google Cloud Run (the API)

**Console:** https://console.cloud.google.com/run · **Free tier:** https://cloud.google.com/run

You need the `gcloud` CLI: https://cloud.google.com/sdk/docs/install

```bash
gcloud auth login
gcloud projects create portfolio-api-prod --name="Portfolio API"
gcloud config set project portfolio-api-prod
```

Cloud Run requires billing to be enabled on the project even to use the free grant. It will not
charge at this traffic — 180,000 vCPU-seconds and 2M requests a month is far beyond ~100 visitors —
but the card has to be on file. Set a budget alert at $1 so any surprise is loud:
https://console.cloud.google.com/billing/budgets

```bash
gcloud services enable run.googleapis.com artifactregistry.googleapis.com
```

**Create a registry and push the image.** The image builds from the repository root because it needs
both the solution and `content/`:

```bash
gcloud artifacts repositories create portfolio --repository-format=docker --location=us-central1
```

```bash
gcloud auth configure-docker us-central1-docker.pkg.dev
```

From the repository root:

```bash
docker build -f infra/docker/api.Dockerfile -t us-central1-docker.pkg.dev/portfolio-api-prod/portfolio/api:v1 .
```

```bash
docker push us-central1-docker.pkg.dev/portfolio-api-prod/portfolio/api:v1
```

Tag by version or commit SHA, never only `latest` — a rollback needs something to roll back to.

**Deploy**, substituting the connection string from step 1 and the real domain:

```bash
gcloud run deploy portfolio-api --image us-central1-docker.pkg.dev/portfolio-api-prod/portfolio/api:v1 --region us-central1 --platform managed --allow-unauthenticated --port 8080 --min-instances 0 --max-instances 3 --memory 512Mi --cpu 1 --set-env-vars "Portfolio__Database__Enabled=true,Portfolio__Database__ConnectionString=<the Npgsql string>,Portfolio__Cors__AllowedOrigins__0=https://<your-domain>"
```

Notes on those flags:

- `--min-instances 0` is what makes it free. It also means the first request after idle pays a cold
  start — which the prerendered frontend hides, and which is exactly why ADR-0002 was worth doing.
- `--max-instances 3` caps the blast radius of anything unexpected.
- `--allow-unauthenticated` because the API serves public content.
- The double underscores are how .NET maps environment variables onto configuration sections;
  `Portfolio__Cors__AllowedOrigins__0` is the first element of that array.

Cloud Run prints a URL like `https://portfolio-api-xxxxx-uc.a.run.app`. Check it:

```bash
curl -s https://portfolio-api-xxxxx-uc.a.run.app/health/ready
```

A healthy response lists both languages. If it reports unhealthy, the connection string is the first
thing to check — `gcloud run services logs read portfolio-api --region us-central1` will say so
plainly.

---

## Step 3 — Cloudflare Pages (the site)

**Dashboard:** https://dash.cloudflare.com · **Docs:** https://developers.cloudflare.com/pages/

1. Sign up at https://dash.cloudflare.com/sign-up.
2. **Workers & Pages → Create → Pages → Connect to Git**, and authorise the `luok4n/Portafolio`
   repository.
3. Build settings:

| Setting | Value |
|---|---|
| Production branch | `main` |
| Root directory | `src/frontend/portfolio-web` |
| Build command | `npm ci && npm run build` |
| Build output directory | `dist/portfolio-web/browser` |
| Environment variable | `NODE_VERSION` = `24` |

4. **This is where decision A or B from the top matters.** With option B the build works as
   configured. With option A, skip the git integration and deploy from GitHub Actions instead:

```bash
npx wrangler pages deploy dist/portfolio-web/browser --project-name=portfolio
```

That needs a Cloudflare API token with the *Cloudflare Pages — Edit* permission, created at
https://dash.cloudflare.com/profile/api-tokens and stored as a GitHub Actions secret. The `images`
job already produces the CVs and the preview images, so the deploy step goes after it.

5. **Custom domain.** Pages → your project → *Custom domains* → *Set up a domain*. Cloudflare issues
   the TLS certificate automatically; there is no certificate to install or renew. If the domain is
   registered elsewhere, point its nameservers at Cloudflare or add the `CNAME` the dashboard shows.

Cloudflare Registrar sells domains at cost with no markup, which is the simplest option if the
domain is not bought yet: https://dash.cloudflare.com/?to=/:account/domains/register

---

## Step 4 — Wire the two together

The one place the deployed topology differs from local, as recorded in ADR-0006: locally nginx makes
everything same-origin, so CORS is empty. Deployed, the frontend and the API are separate origins.

1. The API already has the site's origin in `Portfolio__Cors__AllowedOrigins__0` from step 2.
2. The frontend calls `/api/content` as a relative path — which, on a static host, resolves to
   Cloudflare rather than to Cloud Run. Point it at the API's origin, either by making the base URL
   configurable in `content.service.ts` or by adding a Pages redirect rule that proxies `/api/*` to
   the Cloud Run URL.

**The site works either way.** If the call fails, the page still renders completely from its
embedded snapshot and shows the cached-content notice. That is the difference between a
misconfiguration you fix on Monday and a portfolio that was blank when a recruiter opened it.

---

## Step 5 — Verify

```bash
curl -s -o /dev/null -w '%{http_code}\n' https://<your-domain>/en/
```

Then the same list CI already checks on every push:

| Check | Expected |
|---|---|
| `/en/` and `/es/` | 200 |
| `/en/engineering/` and `/es/ingenieria/` | 200 |
| `/en/projects/slang/` | 200 |
| `/cv/Sebastian_Velez_CV_EN.pdf` | 200 — **the one most likely to be missing** |
| `/og/og-en.png` | 200 |
| `/sitemap.xml` | 200, and every URL inside it resolves |
| `/en/definitely-not-a-page` | **404**, not 200 |
| `<link rel="canonical">` | points at the real domain, not the placeholder |

Paste the URL into LinkedIn's post composer to confirm the preview card renders — that is what the
Open Graph work was for, and it is the only way to see it actually working.

---

## Cost, and how to stop paying

At this traffic all three stay inside their free tiers. Worth confirming once, a month after the
first real visitors:

- Cloud Run usage: https://console.cloud.google.com/run — check vCPU-seconds against the 180,000
  free.
- Neon compute hours: the project dashboard — 100 CU-hours a month, and it sleeps after 5 minutes.
- Cloudflare: bandwidth is unlimited on Pages, so there is nothing to watch.

To take everything down:

```bash
gcloud run services delete portfolio-api --region us-central1
```

```bash
gcloud projects delete portfolio-api-prod
```

Delete the Neon project from its dashboard, and the Pages project from Workers & Pages. Nothing
outlives that, and nothing keeps billing.

---

## Still open

- The `SITE_ORIGIN` constant, and whether it becomes an environment variable.
- Decision A or B for the generated assets.
- Whether `/api` is proxied through Pages or called cross-origin.
- A CD workflow. CI builds the images already; it does not push or deploy them, because until now
  there was nowhere to push to.
