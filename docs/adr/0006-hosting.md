# ADR-0006 — Hosting, and whether Kubernetes is justified

- **Status:** Accepted
- **Date:** 2026-08-25
- **Prices checked:** 2026-08-25. They move; re-verify before creating anything.

## Context

The system is ready to deploy: a prerendered static frontend, a stateless read-only API, and
PostgreSQL. The development plan deliberately left this decision until now so it could be made with
the real thing in hand rather than as an assumption at the start.

Two numbers decide almost everything.

**Expected traffic: roughly 100 visitors a month**, and they are recruiters, reached by a link the
author sends. That is about three visits a day. Any two options that both work are separated by
cost and by operational burden, not by capacity — nothing here is a scaling problem.

**The frontend already serves the whole site with no backend.** ADR-0002 embeds a content snapshot
in the bundle and prerenders every route, so the API's runtime job is revalidation. That is a
genuine improvement and it is also awkward to say plainly: the question is not "where do we host
the system", it is "how much should we pay for a backend that is, by design, optional at runtime".

The honest answer is: nothing.

## Options considered

### Static frontend

| Option | Cost | Notes |
|---|---|---|
| **Cloudflare Pages** | Free | Unlimited bandwidth, 500 builds/month, free custom domain and TLS, 300+ edge locations. |
| Netlify | Free | 100 GB bandwidth, 300 build minutes/month. Ample here. |
| Azure Static Web Apps | Free | 100 GB bandwidth, custom domain and managed TLS included on the free plan. |
| GitHub Pages | Free | Simple, but no way to proxy `/api`, which breaks the same-origin design. |
| Vercel Hobby | Free | **Commercial use is restricted on the free plan.** A portfolio used to find work sits close enough to that line to be a bad place to build on. |

### API

| Option | Cost at this traffic | Notes |
|---|---|---|
| **Google Cloud Run** | Free | 180,000 vCPU-seconds, 360,000 GiB-seconds and 2M requests free per month. Scales to zero. |
| **Azure Container Apps** | Free | The same free grant per subscription per month, also scale-to-zero. |
| Render free | Free | Spins down after 15 minutes idle; **30–60 second cold start**. At three visits a day, every visitor pays it. |
| Azure App Service F1 | Free | No custom domain with TLS on the free tier — it needs a paid tier or Front Door. |
| Fly.io | — | No longer a realistic free option. |

### PostgreSQL

| Option | Cost | Notes |
|---|---|---|
| **Neon** | Free | 0.5 GB storage, 100 compute-hours/month. Scales to zero after 5 minutes idle and **resumes in about a second**. Built for exactly this shape of traffic. |
| Supabase | Free | 500 MB, but **pauses the project entirely after a week of inactivity and needs a manual unpause**. A portfolio can easily go a fortnight without a visit; the failure mode is a dead site discovered by the recruiter, not by the author. Disqualifying. |
| Azure Database for PostgreSQL | Free for 12 months, then paid | A trial, not a free tier. Fine to use, wrong to depend on. |

### Run everything on one small server

| Option | Cost | Notes |
|---|---|---|
| Hetzner CX22 | €4.49/month | 2 vCPU, 4 GB, 20 TB traffic. `docker compose up` and the deployed topology is byte-for-byte what is tested locally. |
| Oracle Cloud Always Free | Free | **Ruled out.** In June 2026 Oracle halved the Always Free Ampere allowance with no announcement and began terminating instances above the new limit in August. It also reclaims instances whose 95th-percentile CPU stays under 20% over seven days — which describes this site exactly. Free infrastructure that deletes idle workloads without warning is the wrong foundation for the link on a CV. |

### Kubernetes

| Option | Realistic monthly cost | Notes |
|---|---|---|
| AWS EKS | ~$73 control plane **before any node** | |
| DigitalOcean / Civo | ~$36 | Free control plane, but two small nodes plus a load balancer. |
| AKS / GKE | ~$30–40 | Free control-plane tier; the nodes and the load balancer are the cost. |
| k3s on a Hetzner VPS | ~€5 | Real Kubernetes, but a single node — it demonstrates the manifests, not the orchestration. |
| kind / minikube locally | Free | Same manifests, same probes, no cluster to pay for or patch. |

## Decision

### Production

**Cloudflare Pages + Google Cloud Run + Neon. Cost: $0/month, plus roughly $10–15 a year for the
domain.**

- Cloudflare Pages for the static site: unlimited bandwidth means the one scenario that could
  produce a surprise bill — the link doing unexpectedly well — cannot.
- Cloud Run for the API: scales to zero, and at three visits a day the free grant is not close to
  being consumed. Azure Container Apps is an equivalent second choice on identical numbers, and the
  API is a container either way, so switching is a redeploy rather than a rewrite.
- Neon for PostgreSQL: the only free tier whose idle behaviour is a one-second resume rather than a
  manual unpause.

### Kubernetes: not in production, and the manifests stay

For serving this site, Kubernetes is not justified. The cheapest credible managed cluster is around
$36/month to serve about 100 visitors — roughly **$0.36 per visitor** for a page that a CDN serves
for nothing. It would also add a control plane, node upgrades and a load balancer to a system whose
whole runtime is a folder of HTML and one stateless container. Choosing it would be picking the
answer that sounds impressive over the one the numbers support, which is the opposite of what this
project is meant to demonstrate.

Phase 12 therefore changes shape rather than disappearing. The manifests are written and **run on a
local cluster** — Deployments, Services, ConfigMaps, Secrets, Ingress, probes wired to the real
health endpoints, resource requests and limits. They are exercised with `kind`, documented, and
explicitly labelled as not the production path.

That is a better answer in an interview than the alternative. "I ran a Kubernetes cluster for a
personal site" invites the question of why; "I wrote the manifests, ran them locally, and did not
deploy them because it would have cost thirty-six dollars a month to serve a hundred visitors"
answers it in advance.

### The one place the deployed topology differs from local

Locally, nginx serves the static files and proxies `/api`, so the browser sees a single origin and
the API runs with an empty CORS allow-list. On a static host the frontend and the API are separate
origins.

The API will therefore run with the site's origin configured in `Portfolio:Cors:AllowedOrigins`.
The setting already exists and is already tested; nothing is redesigned. It is recorded here because
it is a real difference between what is tested and what is deployed, and differences like that are
worth writing down rather than discovering later.

The alternative — proxying `/api` through a Cloudflare Pages Function to preserve same-origin — is
kept in reserve if the CORS surface ever becomes a problem. It is not worth the extra moving part
today.

## Consequences

**Positive**
- Nothing to pay and nothing to switch off. A portfolio whose hosting bill outlives the job search
  is a portfolio that gets taken down.
- Nothing to patch: no VM, no cluster, no operating system.
- No surprise bill is possible on the path a link takes when it does well.
- The frontend keeps working even when the API is cold or asleep, which is what makes a
  scale-to-zero backend acceptable rather than a liability.

**Negative**
- Three vendors instead of one, each with its own account, dashboard and free-tier rules.
- Free tiers change, and this one has already demonstrated it: Oracle halved theirs mid-year without
  telling anyone. The mitigation is that all three run a container or static files, so moving is a
  redeploy — but the risk is real and belongs in this list.
- The deployed system is not identical to the tested one: two origins instead of one, and CORS
  enabled. Small, understood, written down.
- Cloud Run and Neon both sleep. The first request after idle pays a resume, which the prerendered
  frontend hides but which is nonetheless there.
- Kubernetes will exist only as manifests running locally, which is less impressive at a glance than
  a live cluster. That is the correct trade and the reasoning is above.

**Deferred to phase 13**
- Domain registration, DNS, and TLS. All three platforms issue certificates automatically; there is
  no manual certificate handling to plan for.
- A cost alert is not applicable at $0, but the free-tier consumption on Cloud Run and Neon is worth
  checking once after a month of real traffic to confirm the estimate above.

## Sources

Checked 2026-08-25. All of these change without notice.

- [Cloudflare Pages / Workers pricing](https://developers.cloudflare.com/pages/functions/pricing/)
- [Google Cloud Run](https://cloud.google.com/run) — free grant per month
- [Azure Container Apps pricing](https://azure.microsoft.com/en-us/pricing/details/container-apps/)
- [Azure Static Web Apps pricing](https://azure.microsoft.com/en-us/pricing/details/app-service/static/)
- [Neon free plan](https://neon.com/pricing)
- [Supabase pricing](https://supabase.com/pricing) — free-project pausing
- [Render free tier behaviour](https://render.com/articles/platforms-with-a-real-free-tier-for-developers-in-2026)
- [Oracle Always Free resources](https://docs.oracle.com/en-us/iaas/Content/FreeTier/freetier_topic-Always_Free_Resources.htm)
  and [InfoQ on the June 2026 reduction](https://www.infoq.com/news/2026/07/oracle-cloud-free-tier-limits/)
- [Hetzner Cloud pricing](https://www.hetzner.com/cloud)
- [Amazon EKS pricing](https://aws.amazon.com/eks/pricing/)
