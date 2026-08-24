/**
 * Writes sitemap.xml and robots.txt from what was actually prerendered.
 *
 * Generated from the build output rather than from the route table, so the sitemap lists exactly the
 * pages that exist. A hand-maintained list drifts the first time a route is added, and a sitemap
 * full of 404s is worse than no sitemap — it is a signal of neglect that search engines act on.
 *
 * The origin is read from the canonical tag of the prerendered English home page, for the same
 * reason: the sitemap and the canonical URLs cannot disagree if only one of them decides.
 *
 * Runs automatically after `ng build` through the npm postbuild hook.
 */

import { readFileSync, writeFileSync, readdirSync, statSync, existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, relative, resolve, sep } from 'node:path';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const outDir = join(root, 'dist', 'portfolio-web', 'browser');

function readOrigin() {
  const home = join(outDir, 'en', 'index.html');
  const html = readFileSync(home, 'utf8');
  const match = /<link[^>]+rel="canonical"[^>]+href="(https?:\/\/[^/"]+)/i.exec(html);

  if (!match) {
    throw new Error('No canonical tag in en/index.html — cannot determine the site origin.');
  }

  return match[1];
}

/** Every directory holding an index.html, as a site-absolute path. */
function collectPages() {
  const pages = [];

  const walk = (dir) => {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      const path = join(dir, entry.name);
      if (entry.isDirectory()) {
        walk(path);
      } else if (entry.name === 'index.html') {
        const rel = relative(outDir, dir).split(sep).filter(Boolean).join('/');
        pages.push({ path: rel ? `/${rel}` : '/', file: path });
      }
    }
  };

  walk(outDir);
  return pages;
}

/**
 * The locale entry point and the 404 pages are excluded. The first is a redirect hop that carries
 * `noindex`; the second is an error document. Listing either in a sitemap asks a crawler to index a
 * page that tells it to go away.
 */
const EXCLUDED = /^\/$|\/404$/;

/**
 * Reads the alternates the page already declares.
 *
 * Pairing pages by position is the obvious approach and it is wrong: path segments are translated,
 * so `/en/engineering` and `/es/ingenieria` share nothing to match on, and matching by segment count
 * happily paired `/en/engineering` with `/es/404`. Every prerendered page already carries correct
 * reciprocal `hreflang` links written by the application, so the sitemap reads those rather than
 * inventing a second, worse answer to the same question.
 */
function alternatesFor(page, origin) {
  const html = readFileSync(page.file, 'utf8');
  const alternates = {};

  for (const match of html.matchAll(/<link[^>]+rel="alternate"[^>]+hreflang="([a-z-]+)"[^>]+href="([^"]+)"/gi)) {
    const [, hreflang, href] = match;
    if (hreflang === 'x-default') {
      continue;
    }

    // Stored as a site-absolute path so the sitemap emits one origin, whatever the page recorded.
    alternates[hreflang] = href.startsWith(origin) ? href.slice(origin.length) : href;
  }

  return alternates;
}

function writeSitemap(origin, pages) {
  const indexable = pages.filter((p) => !EXCLUDED.test(p.path));

  const entries = indexable.map((page) => {
    const alternates = alternatesFor(page, origin);
    const lastMod = statSync(page.file).mtime.toISOString().slice(0, 10);

    // Home pages are the entry point and change whenever anything does; detail pages do not.
    const priority = page.path.split('/').filter(Boolean).length === 1 ? '1.0' : '0.7';

    const links = Object.entries(alternates)
      .map(([locale, path]) =>
        `    <xhtml:link rel="alternate" hreflang="${locale}" href="${origin}${path}"/>`)
      .join('\n');

    return [
      '  <url>',
      `    <loc>${origin}${page.path}</loc>`,
      `    <lastmod>${lastMod}</lastmod>`,
      `    <priority>${priority}</priority>`,
      links,
      '  </url>',
    ].filter(Boolean).join('\n');
  });

  const xml = [
    '<?xml version="1.0" encoding="UTF-8"?>',
    '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"',
    '        xmlns:xhtml="http://www.w3.org/1999/xhtml">',
    ...entries,
    '</urlset>',
    '',
  ].join('\n');

  writeFileSync(join(outDir, 'sitemap.xml'), xml, 'utf8');
  console.log(`  ok    sitemap.xml (${indexable.length} pages, ${pages.length - indexable.length} excluded)`);
}

function writeRobots(origin) {
  const robots = [
    '# The whole site is meant to be found. The only things closed off are the API, which serves the',
    '# same content a crawler already has from the HTML, and the redirect hop at the root.',
    'User-agent: *',
    'Allow: /',
    'Disallow: /api/',
    '',
    `Sitemap: ${origin}/sitemap.xml`,
    '',
  ].join('\n');

  writeFileSync(join(outDir, 'robots.txt'), robots, 'utf8');
  console.log('  ok    robots.txt');
}

// Last, not first. Function declarations hoist but the const tables above do not, so running at the
// top reads fine and throws.
if (!existsSync(outDir)) {
  console.error(`  FAIL  no build output at ${outDir}`);
  process.exitCode = 1;
} else {
  const origin = readOrigin();
  const pages = collectPages();
  writeSitemap(origin, pages);
  writeRobots(origin);
}
