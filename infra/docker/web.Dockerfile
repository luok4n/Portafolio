# Portfolio web — Angular prerendered to static files, served by nginx.
# Build context is the repository root.
#
#   docker build -f infra/docker/web.Dockerfile -t portfolio-web .
#
# No Node process reaches the runtime image. ADR-0002 prerenders every route at build time, so what
# ships is HTML, CSS, JS and a web server — nothing that can crash at 3am.

# --- build ---------------------------------------------------------------------------------------
FROM node:24-alpine AS build
WORKDIR /src

COPY src/frontend/portfolio-web/package*.json ./
# `npm ci` rather than `install`: the lockfile decides, so the image cannot quietly get a different
# dependency tree than the machine it was tested on.
RUN npm ci

COPY src/frontend/portfolio-web/ ./

# The downloadable CV is generated on the host or in CI by tools/cv/build-cv.mjs, which needs a
# headless browser this image has no business carrying. Fail loudly rather than shipping a site
# whose "Download CV" button 404s — a silently missing asset is the kind of defect nobody notices
# until a recruiter clicks it.
RUN test -f public/cv/Sebastian_Velez_CV_EN.pdf && test -f public/cv/Sebastian_Velez_CV_ES.pdf \
    && test -f public/og/og-en.png && test -f public/og/og-es.png \
    || (echo "ERROR: public/cv or public/og is empty. Run 'npm run content' before building this image." && exit 1)

# The content snapshot is committed, so prerendering needs no API and no network here. That is the
# point of embedding it: the image builds in a sealed environment.
RUN npm run build

# --- runtime -------------------------------------------------------------------------------------
FROM nginx:1.27-alpine AS runtime

COPY infra/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY infra/docker/security-headers.conf /etc/nginx/snippets/security-headers.conf
COPY --from=build /src/dist/portfolio-web/browser /usr/share/nginx/html

# Runs as nginx rather than root. The config already listens on 8080 because an unprivileged process
# cannot bind port 80.
RUN chown -R nginx:nginx /usr/share/nginx/html /var/cache/nginx /var/log/nginx \
    && touch /var/run/nginx.pid && chown nginx:nginx /var/run/nginx.pid

EXPOSE 8080
USER nginx

HEALTHCHECK --interval=15s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://127.0.0.1:8080/en/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
