/**
 * Builds the CV from content/ as HTML, then converts it to PDF with headless Chrome.
 *
 * The CV is generated rather than hand-edited so that it can never drift from the portfolio
 * content, and so that regenerating it is a reproducible command instead of manual work in a
 * word processor.
 *
 * Four files are produced — two locales times two variants:
 *   - public : no phone number. This is the one published on the site. See ADR-0003.
 *   - full   : includes the phone number, read from content/private/contact.local.json (untracked).
 *              Skipped when that file is absent, e.g. on CI or a fresh clone.
 *
 * Usage:  node tools/cv/build-cv.mjs [--html-only] [--locale en|es]
 */

import { readFileSync, existsSync, mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';
import { tmpdir } from 'node:os';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..');
const contentDir = join(root, 'content');
const outDir = join(root, 'dist', 'cv');

// A two-page CV gets read; a three-page one gets skimmed.
const MAX_PAGES = 2;
const MIN_SCALE = 0.82;

const readJson = (name) => JSON.parse(readFileSync(join(contentDir, name), 'utf8'));

// *.en.json is the base locale: it owns ids, dates, technologies and sources. Translation files
// carry only translatable fields, matched by id, so no fact is stored twice.
const base = {
  profile: readJson('profile.en.json'),
  experience: readJson('experience.en.json').experience,
  projects: readJson('projects.en.json').projects,
  education: readJson('education.en.json').education,
};
const translations = {
  es: {
    profile: readJson('profile.es.json'),
    experience: readJson('experience.es.json').experience,
    projects: readJson('projects.es.json').projects,
    education: readJson('education.es.json').education,
  },
};

const { categories } = readJson('skills.json');
const { links } = readJson('social-links.json');

const privatePath = join(contentDir, 'private', 'contact.local.json');
const privateContact = existsSync(privatePath) ? readJson('private/contact.local.json') : null;

// --- locale labels ------------------------------------------------------------------------------
const LABELS = {
  en: {
    lang: 'en',
    months: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
    summary: 'Professional Summary',
    experience: 'Professional Experience',
    skills: 'Technical Skills',
    education: 'Education',
    languages: 'Languages',
    projects: 'Featured Projects',
  },
  es: {
    lang: 'es',
    months: ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'],
    summary: 'Resumen Profesional',
    experience: 'Experiencia Profesional',
    skills: 'Habilidades Técnicas',
    education: 'Educación',
    languages: 'Idiomas',
    projects: 'Proyectos Destacados',
  },
};

// --- years of experience -------------------------------------------------------------------------
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

const tenure = yearsOfExperience(base.experience);

// --- locale resolution ---------------------------------------------------------------------------
function resolveLocale(locale) {
  const t = translations[locale];
  const pick = (baseItem, key) => {
    if (!t) return baseItem;
    const tr = t[key].find((x) => x.id === baseItem.id);
    return tr ? { ...baseItem, ...tr } : baseItem;
  };
  return {
    profile: { ...base.profile, ...(t?.profile ?? {}) },
    experience: base.experience.map((r) => pick(r, 'experience')),
    projects: base.projects.map((p) => pick(p, 'projects')),
    education: base.education.map((e) => pick(e, 'education')),
  };
}

// --- formatting ----------------------------------------------------------------------------------
const esc = (s) => String(s).replace(/[&<>]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c]));
const linkFor = (id) => links.find((l) => l.id === id);

function render({ locale, includePhone, scale = 1 }) {
  const L = LABELS[locale];
  const { profile, experience, projects, education } = resolveLocale(locale);
  const fmt = (ym) => {
    const [y, m] = ym.split('-').map(Number);
    return `${L.months[m - 1]} ${y}`;
  };

  const summary = profile.summaryTemplate.replace('{years}', String(tenure.years));
  const contactBits = [profile.location];
  if (includePhone && privateContact?.phone) contactBits.push(privateContact.phone);
  contactBits.push(base.profile.email);

  const featured = projects.filter((p) => base.projects.find((b) => b.id === p.id)?.featured);

  return `<!doctype html>
<html lang="${L.lang}">
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
    font-size: ${(9.2 * scale).toFixed(2)}pt; line-height: 1.28; color: #1a1a1a; margin: 0;
    -webkit-print-color-adjust: exact; print-color-adjust: exact;
  }
  header { text-align: center; margin-bottom: 9pt; }
  h1 { font-size: 18pt; margin: 0 0 2pt; font-weight: 700; letter-spacing: .2pt; }
  .headline { font-size: 10.5pt; font-weight: 600; color: #2a2a2a; margin-bottom: 2pt; }
  .contact { font-size: 9pt; color: #333; }
  .contact a { color: #1a4f8a; text-decoration: none; }
  h2 {
    font-size: 10.5pt; font-weight: 700; text-transform: uppercase; letter-spacing: .4pt;
    margin: ${(9 * scale).toFixed(2)}pt 0 3.5pt; padding-bottom: 1.5pt; border-bottom: .8pt solid #1a4f8a; color: #1a4f8a;
  }
  section { break-inside: auto; }
  .role { break-inside: avoid; margin-bottom: ${(5.5 * scale).toFixed(2)}pt; }
  .role-head { display: flex; justify-content: space-between; gap: 8pt; align-items: baseline; }
  .role-title, .role-company { font-weight: 700; }
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
  <h2>${esc(L.summary)}</h2>
  <p class="summary">${esc(summary)}</p>
</section>

<section>
  <h2>${esc(L.experience)}</h2>
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
  <h2>${esc(L.skills)}</h2>
  ${categories.map((c) => `<p><span class="label">${esc(c.label[locale])}:</span> ${esc(c.items.join(', '))}</p>`).join('')}
</section>

<section>
  <h2>${esc(L.education)}</h2>
  ${education.map((e) => `<div class="edu"><div>${esc(e.degree)} &mdash; ${esc(e.institution)}</div><div class="role-dates">${e.year}</div></div>`).join('')}
</section>

<section>
  <h2>${esc(L.languages)}</h2>
  <p>${profile.languages.map((l) => `${esc(l.language)}: ${esc(l.level)}`).join(' &nbsp;|&nbsp; ')}</p>
</section>

<section class="projects">
  <h2>${esc(L.projects)}</h2>
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

// --- Chrome ---------------------------------------------------------------------------------------
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

// Counts /Type /Page objects, ignoring the /Pages tree node.
function pageCount(pdfPath) {
  const raw = readFileSync(pdfPath, 'latin1');
  return (raw.match(/\/Type\s*\/Page(?![s])/g) ?? []).length;
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

// --- build ----------------------------------------------------------------------------------------
mkdirSync(outDir, { recursive: true });

const htmlOnly = process.argv.includes('--html-only');
const localeArg = process.argv[process.argv.indexOf('--locale') + 1];
const locales = process.argv.includes('--locale') ? [localeArg] : ['en', 'es'];

const chrome = htmlOnly ? null : findChrome();
if (!chrome && !htmlOnly) {
  console.error('No Chrome or Edge found. Set CHROME_PATH, or run with --html-only.');
  process.exit(1);
}

console.log(`Experience: ${tenure.months} unique months worked -> ${tenure.years}+ years\n`);

for (const locale of locales) {
  if (!LABELS[locale]) {
    console.error(`Unknown locale "${locale}".`);
    process.exit(1);
  }
  for (const variant of [{ suffix: 'public', includePhone: false, always: true },
                         { suffix: 'full', includePhone: true, always: false }]) {
    if (!variant.always && !privateContact) {
      console.log(`- ${locale}/${variant.suffix}: skipped (content/private/contact.local.json not present)`);
      continue;
    }
    const name = `Sebastian_Velez_CV_${locale.toUpperCase()}_${variant.suffix}`;
    const htmlPath = join(outDir, `${name}.html`);
    const pdfPath = join(outDir, `${name}.pdf`);

    if (!chrome) {
      writeFileSync(htmlPath, render({ locale, includePhone: variant.includePhone }), 'utf8');
      console.log(`- ${name}.html`);
      continue;
    }

    // Fit to MAX_PAGES by measurement rather than by eye. Spanish runs roughly 15% longer than
    // English for the same content, so a single hand-tuned font size cannot serve both locales —
    // and it would need re-tuning every time a bullet is added.
    let scale = 1;
    let pages = 0;
    for (let attempt = 0; attempt < 8; attempt++) {
      writeFileSync(htmlPath, render({ locale, includePhone: variant.includePhone, scale }), 'utf8');
      htmlToPdf(chrome, htmlPath, pdfPath);
      pages = pageCount(pdfPath);
      if (pages <= MAX_PAGES || scale <= MIN_SCALE) break;
      scale = Number((scale - 0.03).toFixed(2));
    }

    const fit = pages <= MAX_PAGES ? '' : `  ** still ${pages} pages at minimum scale — trim content **`;
    console.log(`- ${name}  (${pages} page${pages === 1 ? '' : 's'}, scale ${scale.toFixed(2)})${fit}`);
  }
}
