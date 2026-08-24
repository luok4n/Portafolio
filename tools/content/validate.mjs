/**
 * Checks the content files against each other.
 *
 * Translations drift silently: a bullet added to the English experience and forgotten in Spanish
 * produces a page that is subtly shorter in one language and nobody notices for months. This turns
 * that into a build failure.
 *
 * Usage:  node tools/content/validate.mjs
 * Exits non-zero if anything is wrong, so CI can gate on it.
 */

import { readFileSync, existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..');
const contentDir = join(root, 'content');
const readJson = (name) => JSON.parse(readFileSync(join(contentDir, name), 'utf8'));

const errors = [];
const warnings = [];

const fail = (msg) => errors.push(msg);
const warn = (msg) => warnings.push(msg);

// --- experience --------------------------------------------------------------------------------
const en = readJson('experience.en.json').experience;
const es = readJson('experience.es.json').experience;

const enIds = en.map((r) => r.id);
const esIds = es.map((r) => r.id);

for (const id of enIds) if (!esIds.includes(id)) fail(`experience: "${id}" has no Spanish translation`);
for (const id of esIds) if (!enIds.includes(id)) fail(`experience: Spanish "${id}" has no base-locale entry`);

for (const role of en) {
  const t = es.find((r) => r.id === role.id);
  if (!t) continue;
  if (t.highlights?.length !== role.highlights.length) {
    fail(`experience "${role.id}": ${role.highlights.length} highlights in English, ${t.highlights?.length ?? 0} in Spanish`);
  }
  if (!t.role) fail(`experience "${role.id}": missing translated role title`);
}

// --- dates -------------------------------------------------------------------------------------
const ym = /^\d{4}-(0[1-9]|1[0-2])$/;
for (const role of en) {
  if (!ym.test(role.start)) fail(`experience "${role.id}": bad start "${role.start}"`);
  if (!ym.test(role.end)) fail(`experience "${role.id}": bad end "${role.end}"`);
  if (role.start > role.end) fail(`experience "${role.id}": starts after it ends`);
}

// --- projects ----------------------------------------------------------------------------------
const pEn = readJson('projects.en.json').projects;
const pEs = readJson('projects.es.json').projects;
const pEnIds = pEn.map((p) => p.id);

for (const id of pEnIds) if (!pEs.some((p) => p.id === id)) fail(`projects: "${id}" has no Spanish translation`);
for (const p of pEs) if (!pEnIds.includes(p.id)) fail(`projects: Spanish "${p.id}" has no base-locale entry`);

for (const p of pEn) {
  if (!enIds.includes(p.experienceId)) fail(`project "${p.id}": experienceId "${p.experienceId}" matches no role`);
  if (p.verified && (!p.sources || p.sources.length === 0)) fail(`project "${p.id}": marked verified but cites no source`);
  if (!p.verified && !p.identityStatus) warn(`project "${p.id}": unverified — described by function only`);
  if (p.identityStatus) warn(`project "${p.id}": ${p.identityStatus} — not publishable yet`);
  if (p.contribution === null) warn(`project "${p.id}": contribution still empty`);
}

// --- every project referenced by a role exists, and vice versa -----------------------------------
for (const role of en) {
  for (const pid of role.projects ?? []) {
    if (!pEnIds.includes(pid)) fail(`experience "${role.id}": references unknown project "${pid}"`);
  }
}
for (const p of pEn) {
  const role = en.find((r) => r.id === p.experienceId);
  if (role && !(role.projects ?? []).includes(p.id)) {
    fail(`project "${p.id}": role "${p.experienceId}" does not list it back`);
  }
}

// --- drafts still awaiting approval ---------------------------------------------------------------
for (const role of en) {
  if (role.highlightsStatus) warn(`experience "${role.id}": ${role.highlightsStatus}`);
}

// --- engineering section -------------------------------------------------------------------------
// Ships with the app rather than through the API (ADR-0005), so nothing else checks it.
if (existsSync(join(contentDir, 'engineering.en.json'))) {
  const engEn = readJson('engineering.en.json');
  const engEs = readJson('engineering.es.json');

  const idsOf = (doc, key) => (doc[key] ?? []).map((x) => x.id);
  for (const key of ['decisions', 'flows']) {
    const a = idsOf(engEn, key);
    const b = idsOf(engEs, key);
    for (const id of a) if (!b.includes(id)) fail(`engineering.${key}: "${id}" has no Spanish translation`);
    for (const id of b) if (!a.includes(id)) fail(`engineering.${key}: Spanish "${id}" has no English entry`);
  }

  for (const key of ['testing', 'operations']) {
    if ((engEn[key]?.items?.length ?? 0) !== (engEs[key]?.items?.length ?? 0)) {
      fail(`engineering.${key}: item counts differ between languages`);
    }
  }

  // Every {placeholder} the copy uses must be a fact the collector actually produces. A placeholder
  // with no matching fact would render as literal braces on a public page.
  if (!existsSync(join(contentDir, 'engineering-facts.json'))) {
    fail('engineering-facts.json is missing — run node tools/engineering/collect-facts.mjs');
  } else {
    const facts = readJson('engineering-facts.json');
    for (const [name, doc] of [['en', engEn], ['es', engEs]]) {
      const placeholders = new Set([...JSON.stringify(doc).matchAll(/\{([a-zA-Z]+)\}/g)].map((m) => m[1]));
      for (const p of placeholders) {
        if (!(p in facts)) fail(`engineering.${name}: "{${p}}" matches no generated fact`);
      }
    }

    // A number written into the copy is a number that will go stale. The generated facts exist so
    // that never happens; this catches someone taking the shortcut later.
    for (const [name, doc] of [['en', engEn], ['es', engEs]]) {
      const prose = Object.entries(doc)
        .filter(([k]) => !k.startsWith('$'))
        .map(([, v]) => JSON.stringify(v))
        .join(' ');
      const suspicious = prose.match(/\b\d{2,}\s+(tests|tables|endpoints|decisions|pruebas|tablas|endpoints|decisiones)\b/gi);
      if (suspicious) fail(`engineering.${name}: hardcoded count "${suspicious[0]}" — use a {placeholder}`);
    }
  }
}

// --- privacy ----------------------------------------------------------------------------------
// A phone number must never appear in a tracked content file.
const tracked = ['profile.en.json', 'profile.es.json', 'social-links.json', 'cv-source.md', 'experience.en.json', 'experience.es.json'];
const phoneish = /\+?\s*\(?\+?57\)?[\s.-]*\d{3}[\s.-]*\d{3}[\s.-]*\d{4}/;
for (const f of tracked) {
  if (!existsSync(join(contentDir, f))) continue;
  if (phoneish.test(readFileSync(join(contentDir, f), 'utf8'))) {
    fail(`${f}: looks like it contains a phone number — see ADR-0003`);
  }
}

// --- report ------------------------------------------------------------------------------------
for (const w of warnings) console.log(`  warn  ${w}`);
for (const e of errors) console.error(`  FAIL  ${e}`);

console.log(`\n${errors.length} error(s), ${warnings.length} warning(s).`);
process.exit(errors.length > 0 ? 1 : 0);
