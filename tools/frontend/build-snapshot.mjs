/**
 * Produces the content the frontend compiles into its bundle.
 *
 * Two things land in src/content-snapshot/:
 *
 *   {locale}.json         the whole portfolio for that language, taken from the running API
 *   engineering.*.json    the engineering section's copy, plus its generated facts
 *
 * The snapshot is fetched from the API rather than re-read from content/ on purpose. Re-reading the
 * files would mean reimplementing the base-locale/translation merge in a third place, and the whole
 * point of the storage seam is that the merge exists once. Fetching also means the snapshot is
 * exactly what the API would have returned — the two cannot describe different content.
 *
 *   node tools/frontend/build-snapshot.mjs
 *
 * Requires the API to be running. That is a deliberate constraint, not an oversight: a build that
 * silently produced a snapshot from a second implementation would be worse than a build that fails.
 */

import { writeFileSync, mkdirSync, readFileSync, existsSync, copyFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..');
const outDir = join(root, 'src', 'frontend', 'portfolio-web', 'src', 'content-snapshot');
const contentDir = join(root, 'content');

const apiBase = process.env.API_BASE ?? 'http://localhost:5080';
const locales = ['en', 'es'];

mkdirSync(outDir, { recursive: true });

let failed = false;

for (const locale of locales) {
  const url = `${apiBase}/api/content?lang=${locale}`;
  try {
    const response = await fetch(url);
    if (!response.ok) throw new Error(`HTTP ${response.status}`);

    const content = await response.json();

    if (!content?.profile?.name || !Array.isArray(content.experience) || content.experience.length === 0) {
      throw new Error('response did not look like portfolio content');
    }

    // The negotiation result is a property of the request, not of the content, and would be
    // misleading baked into a file.
    delete content.language;

    writeFileSync(join(outDir, `${locale}.json`), `${JSON.stringify(content, null, 2)}\n`, 'utf8');
    console.log(`  ok    ${locale}.json  (${content.experience.length} roles, ${content.projects.length} projects)`);
  } catch (error) {
    failed = true;
    console.error(`  FAIL  ${locale}: ${error.message} — is the API running at ${apiBase}?`);
  }
}

// The engineering section ships with the app rather than coming from the API (ADR-0005), so it is
// copied straight across.
for (const file of ['engineering.en.json', 'engineering.es.json', 'engineering-facts.json']) {
  const source = join(contentDir, file);
  if (!existsSync(source)) {
    failed = true;
    console.error(`  FAIL  ${file} is missing — run node tools/engineering/collect-facts.mjs`);
    continue;
  }
  writeFileSync(join(outDir, file), readFileSync(source, 'utf8'), 'utf8');
  console.log(`  ok    ${file}`);
}

// The downloadable CV is generated into the frontend rather than committed: it is an artefact of
// content/, and only the redacted variant may ever be published (ADR-0003). Copying the "full" one
// here would put the phone number into a public bundle.
//
// SKIP_CV=1 is for the CI job that only wants to know whether the committed snapshot is still
// current. It has no browser to render a PDF with, and failing there would say nothing about the
// snapshot.
const skipCv = process.env.SKIP_CV === '1';
const cvDir = join(root, 'src', 'frontend', 'portfolio-web', 'public', 'cv');
if (!skipCv) mkdirSync(cvDir, { recursive: true });

for (const locale of skipCv ? [] : ['EN', 'ES']) {
  const built = join(root, 'dist', 'cv', `Sebastian_Velez_CV_${locale}_public.pdf`);
  if (!existsSync(built)) {
    failed = true;
    console.error(`  FAIL  CV for ${locale} not built — run node tools/cv/build-cv.mjs`);
    continue;
  }
  copyFileSync(built, join(cvDir, `Sebastian_Velez_CV_${locale}.pdf`));
  console.log(`  ok    cv/Sebastian_Velez_CV_${locale}.pdf`);
}

console.log(failed ? '\nSnapshot incomplete.' : '\nSnapshot written to src/content-snapshot/.');
process.exitCode = failed ? 1 : 0;
