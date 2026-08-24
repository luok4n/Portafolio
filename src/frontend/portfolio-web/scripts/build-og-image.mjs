/**
 * Renders the social preview image, one per language.
 *
 * A portfolio travels through LinkedIn and WhatsApp. Without an og:image those platforms show a
 * grey box with a URL, which is the first impression the link makes — so this is not decoration, it
 * is the preview card doing its job.
 *
 * Built from the same content the site serves, with the same tokens, using the headless browser the
 * CV builder already needs. No design tool in the loop and nothing to keep in sync by hand.
 *
 *   node scripts/build-og-image.mjs
 */

import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, writeFileSync, rmSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';
import { tmpdir } from 'node:os';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const snapshotDir = join(root, 'src', 'content-snapshot');
const outDir = join(root, 'public', 'og');

const WIDTH = 1200;
const HEIGHT = 630;

const COPY = {
  en: { years: (n) => `${n}+ years`, sectors: 'energy · fintech · real estate · public health · education' },
  es: { years: (n) => `${n}+ años`, sectors: 'energía · fintech · finca raíz · salud pública · educación' },
};

function findChrome() {
  if (process.env.CHROME_PATH) return process.env.CHROME_PATH;
  const candidates = [
    'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
    '/usr/bin/google-chrome',
    '/usr/bin/chromium',
    '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
  ];
  return candidates.find((c) => existsSync(c)) ?? null;
}

function page(locale) {
  const content = JSON.parse(readFileSync(join(snapshotDir, `${locale}.json`), 'utf8'));
  const { profile } = content;
  const copy = COPY[locale];

  // Dark on purpose: a preview card sits on a light feed in LinkedIn and a dark one in WhatsApp, and
  // dark reads well against both. It is also the only surface where the theme cannot follow the
  // reader, so it has to be chosen rather than inherited.
  return `<!doctype html>
<html lang="${locale}">
<head><meta charset="utf-8">
<style>
  * { box-sizing: border-box; margin: 0; }
  html, body { width: ${WIDTH}px; height: ${HEIGHT}px; }
  body {
    display: flex; flex-direction: column; justify-content: center;
    padding: 76px 84px;
    background: #0e1014;
    color: #e9ecf2;
    font-family: ui-sans-serif, system-ui, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    border-left: 14px solid #79aee6;
  }
  .name { font-size: 74px; font-weight: 700; letter-spacing: -2px; line-height: 1.02; }
  .title { font-size: 38px; font-weight: 600; color: #79aee6; margin-top: 14px; }
  .years { font-size: 27px; color: #98a0b0; margin-top: 34px; }
  .sectors { font-size: 21px; color: #98a0b0; margin-top: 8px; }
  .stack {
    display: flex; gap: 12px; flex-wrap: wrap; margin-top: 40px;
    font-family: ui-monospace, 'Cascadia Mono', Menlo, Consolas, monospace; font-size: 19px;
  }
  .stack span { border: 1px solid #2a2f3a; border-radius: 8px; padding: 7px 15px; color: #98a0b0; }
</style>
</head>
<body>
  <div class="name">${profile.name}</div>
  <div class="title">${profile.title}</div>
  <div class="years">${copy.years(profile.yearsOfExperience)}</div>
  <div class="sectors">${copy.sectors}</div>
  <div class="stack">
    <span>C#</span><span>.NET</span><span>ASP.NET Core</span>
    <span>Azure</span><span>PostgreSQL</span><span>Angular</span>
  </div>
</body>
</html>`;
}

const chrome = findChrome();
if (!chrome) {
  console.error('  FAIL  no Chrome or Edge found. Set CHROME_PATH.');
  process.exitCode = 1;
} else {
  mkdirSync(outDir, { recursive: true });

  for (const locale of Object.keys(COPY)) {
    const htmlPath = join(tmpdir(), `og-${locale}-${process.pid}.html`);
    const pngPath = join(outDir, `og-${locale}.png`);
    const userDataDir = join(tmpdir(), `og-build-${process.pid}-${locale}`);

    writeFileSync(htmlPath, page(locale), 'utf8');

    try {
      execFileSync(chrome, [
        '--headless',
        '--disable-gpu',
        '--no-sandbox',
        '--hide-scrollbars',
        `--window-size=${WIDTH},${HEIGHT}`,
        `--screenshot=${pngPath}`,
        `--user-data-dir=${userDataDir}`,
        `file:///${htmlPath.replace(/\\/g, '/')}`,
      ], { stdio: 'pipe' });

      console.log(`  ok    og/og-${locale}.png`);
    } finally {
      rmSync(userDataDir, { recursive: true, force: true });
      rmSync(htmlPath, { force: true });
    }
  }
}
