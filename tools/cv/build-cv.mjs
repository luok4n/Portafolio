/**
 * Builds the CV from content/ as HTML, then converts it to PDF with headless Chrome.
 *
 * The CV is generated rather than hand-edited so that it can never drift from the portfolio
 * content, and so that regenerating it is a reproducible command instead of manual work in a
 * word processor.
 *
 * Two variants are produced:
 *   - full   : includes the phone number, read from content/private/contact.local.json (untracked).
 *              Skipped when that file is absent, e.g. on CI or a fresh clone.
 *   - public : no phone number. This is the one published on the site. See ADR-0003.
 *
 * Usage:  node tools/cv/build-cv.mjs [--html-only]
 */

import { readFileSync, existsSync, mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';
import { tmpdir } from 'node:os';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..');
const contentDir = join(root, 'content');
const outDir = join(root, 'dist', 'cv');

const readJson = (p) => JSON.parse(readFileSync(p, 'utf8'));

const profile = readJson(join(contentDir, 'profile.en.json'));
const { experience } = readJson(join(contentDir, 'experience.en.json'));
const { categories } = readJson(join(contentDir, 'skills.json'));
const { education } = readJson(join(contentDir, 'education.en.json'));
const { projects } = readJson(join(contentDir, 'projects.en.json'));
const { links } = readJson(join(contentDir, 'social-links.json'));

const privatePath = join(contentDir, 'private', 'contact.local.json');
const privateContact = existsSync(privatePath) ? readJson(privatePath) : null;

// --- years of experience -----------------------------------------------------------------------
// Unique months worked divided by twelve: overlapping roles (the freelance period in 2022) are
// counted once, and a month with two employers still counts as one month.
function yearsOfExperience(roles) {
  const months = new Set();
  for (const role of roles) {
    const [sy, sm] = role.start.split('-').map(Number);
    const [ey, em] = role.end.split('-').map(Number);
    for (let y = sy, m = sm; y < ey || (y === ey && m <= em); m === 12 ? (m = 1, y++) : m++) {
      months.add(`${y}-${String(m).padStart(2, '0')}`);
    }
  }
  return { months: months.size, years: Math.floor(months.size / 12) };
}

const tenure = yearsOfExperience(experience);
const summary = profile.summaryTemplate.replace('{years}', String(tenure.years));

// --- formatting --------------------------------------------------------------------------------
const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const fmt = (ym) => {
  const [y, m] = ym.split('-').map(Number);
  return `${MONTHS[m - 1]} ${y}`;
};
const esc = (s) => String(s).replace(/[&<>]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c]));

const linkFor = (id) => links.find((l) => l.id === id);

function render({ includePhone }) {
  const contactBits = [profile.location];
  if (includePhone && privateContact?.phone) contactBits.push(privateContact.phone);
  contactBits.push(profile.email);

  const featured = projects.filter((p) => p.featured);

  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>${esc(profile.name)} — CV</title>
<style>
  @page { size: Letter; margin: 12mm 14mm; }
  * { box-sizing: border-box; }
  /* Explicit colours: the PDF must not inherit a dark theme from whatever renders the HTML. */
  html, body { background: #ffffff; }
  body {
    font-family: Calibri, Carlito, "Segoe UI", system-ui, sans-serif;
    font-size: 9.2pt; line-height: 1.28; color: #1a1a1a; margin: 0;
    -webkit-print-color-adjust: exact; print-color-adjust: exact;
  }
  header { text-align: center; margin-bottom: 9pt; }
  h1 { font-size: 18pt; margin: 0 0 2pt; font-weight: 700; letter-spacing: .2pt; }
  .headline { font-size: 10.5pt; font-weight: 600; color: #2a2a2a; margin-bottom: 2pt; }
  .contact { font-size: 9pt; color: #333; }
  .contact a { color: #1a4f8a; text-decoration: none; }
  h2 {
    font-size: 10.5pt; font-weight: 700; text-transform: uppercase; letter-spacing: .4pt;
    margin: 9pt 0 3.5pt; padding-bottom: 1.5pt; border-bottom: .8pt solid #1a4f8a; color: #1a4f8a;
  }
  section { break-inside: auto; }
  .role { break-inside: avoid; margin-bottom: 5.5pt; }
  .role-head { display: flex; justify-content: space-between; gap: 8pt; align-items: baseline; }
  .role-title { font-weight: 700; }
  .role-company { font-weight: 700; }
  .role-dates { font-size: 8.8pt; color: #444; white-space: nowrap; }
  ul { margin: 2.5pt 0 0; padding-left: 13pt; }
  li { margin-bottom: 1.2pt; }
  .skills p, .projects p { margin: 0 0 2.5pt; }
  .label { font-weight: 700; }
  .edu { display: flex; justify-content: space-between; gap: 8pt; }
  p { margin: 0; }
  .summary { text-align: justify; }
</style>
</head>
<body>

<header>
  <h1>${esc(profile.name)}</h1>
  <div class="headline">${esc(profile.headline)}</div>
  <div class="contact">${contactBits.map(esc).join(' &nbsp;|&nbsp; ')}</div>
  <div class="contact">
    LinkedIn: <a href="${esc(linkFor('linkedin').url)}">${esc(linkFor('linkedin').display)}</a>
    &nbsp;|&nbsp;
    GitHub: <a href="${esc(linkFor('github').url)}">${esc(linkFor('github').display)}</a>
  </div>
</header>

<section>
  <h2>Professional Summary</h2>
  <p class="summary">${esc(summary)}</p>
</section>

<section>
  <h2>Professional Experience</h2>
  ${experience.map((r) => `
  <div class="role">
    <div class="role-head">
      <div><span class="role-title">${esc(r.role)}</span> &ndash; <span class="role-company">${esc(r.company)}</span></div>
      <div class="role-dates">${fmt(r.start)} &ndash; ${fmt(r.end)}</div>
    </div>
    <ul>${r.highlights.map((h) => `<li>${esc(h)}</li>`).join('')}</ul>
  </div>`).join('')}
</section>

<section class="skills">
  <h2>Technical Skills</h2>
  ${categories.map((c) => `<p><span class="label">${esc(c.label.en)}:</span> ${esc(c.items.join(', '))}</p>`).join('')}
</section>

<section>
  <h2>Education</h2>
  ${education.map((e) => `<div class="edu"><div>${esc(e.degree)} &mdash; ${esc(e.institution)}</div><div class="role-dates">${e.year}</div></div>`).join('')}
</section>

<section>
  <h2>Languages</h2>
  <p>${profile.languages.map((l) => `${esc(l.language)}: ${esc(l.level)}`).join(' &nbsp;|&nbsp; ')}</p>
</section>

<section class="projects">
  <h2>Featured Projects</h2>
  ${featured.map((p) => {
    // A CV and a web page have different length budgets. cvSummary is the short form; the long
    // research-backed summary is for the site.
    const text = p.cvSummary ?? `${p.summary} ${p.contribution ?? ''}`.trim();
    return `<p><span class="label">${esc(p.name)}</span> &mdash; ${esc(text)}</p>`;
  }).join('')}
</section>

</body>
</html>`;
}

// --- Chrome ------------------------------------------------------------------------------------
function findChrome() {
  if (process.env.CHROME_PATH) return process.env.CHROME_PATH;
  const candidates = [
    'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
    'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
    '/usr/bin/google-chrome',
    '/usr/bin/chromium',
    '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
  ];
  return candidates.find((c) => existsSync(c)) ?? null;
}

function htmlToPdf(chrome, htmlPath, pdfPath) {
  const userDataDir = join(tmpdir(), `cv-build-${process.pid}-${Math.random().toString(36).slice(2)}`);
  try {
    execFileSync(chrome, [
      '--headless',
      '--disable-gpu',
      '--no-sandbox',
      '--no-pdf-header-footer',
      '--run-all-compositor-stages-before-draw',
      '--virtual-time-budget=3000',
      `--user-data-dir=${userDataDir}`,
      `--print-to-pdf=${pdfPath}`,
      `file:///${htmlPath.replace(/\\/g, '/')}`,
    ], { stdio: 'pipe' });
  } finally {
    rmSync(userDataDir, { recursive: true, force: true });
  }
}

// --- build -------------------------------------------------------------------------------------
mkdirSync(outDir, { recursive: true });

const variants = [
  { name: 'Sebastian_Velez_CV_public', includePhone: false, always: true },
  { name: 'Sebastian_Velez_CV_full', includePhone: true, always: false },
];

const chrome = process.argv.includes('--html-only') ? null : findChrome();
if (!chrome && !process.argv.includes('--html-only')) {
  console.error('No Chrome or Edge found. Set CHROME_PATH, or run with --html-only.');
  process.exit(1);
}

console.log(`Experience: ${tenure.months} unique months worked -> ${tenure.years}+ years`);

for (const v of variants) {
  if (!v.always && !privateContact) {
    console.log(`- ${v.name}: skipped (content/private/contact.local.json not present)`);
    continue;
  }
  const htmlPath = join(outDir, `${v.name}.html`);
  writeFileSync(htmlPath, render({ includePhone: v.includePhone }), 'utf8');
  console.log(`- ${v.name}.html`);
  if (chrome) {
    const pdfPath = join(outDir, `${v.name}.pdf`);
    htmlToPdf(chrome, htmlPath, pdfPath);
    console.log(`- ${v.name}.pdf`);
  }
}
