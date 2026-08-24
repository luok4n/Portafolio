/**
 * Measures the repository and writes the numbers the engineering section displays.
 *
 * Every figure on that page — tests, tables, endpoints, decisions — is produced here by reading the
 * real artefacts, never typed into a content file. A page that claims fifty tests when there are
 * thirty is worse than no page at all: it is exactly the kind of claim an interviewer checks, and
 * being caught on it puts every other statement on the site in doubt. See
 * docs/adr/0005-engineering-section.md.
 *
 *   node tools/engineering/collect-facts.mjs
 *
 * Writes content/engineering-facts.json. Exits non-zero if a fact cannot be measured, because a
 * missing number must break the build rather than silently become a stale one.
 */

import { readFileSync, writeFileSync, readdirSync, existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..');
const api = join(root, 'src', 'services', 'portfolio-api');

const problems = [];
const read = (path) => (existsSync(path) ? readFileSync(path, 'utf8') : null);
const countMatches = (text, pattern) => (text.match(pattern) ?? []).length;

/** Walks a directory tree returning every file that matches. */
function filesUnder(dir, predicate) {
  if (!existsSync(dir)) return [];
  const out = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) {
      if (entry.name === 'bin' || entry.name === 'obj') continue;
      out.push(...filesUnder(path, predicate));
    } else if (predicate(entry.name)) {
      out.push(path);
    }
  }
  return out;
}

// --- tests -------------------------------------------------------------------------------------
// xUnit runs one case per [Fact] (including the Docker-gated variant) and one per [InlineData] on a
// [Theory], which is the number the runner reports. Counting [Theory] itself would undercount.
function countDotnetTests() {
  const files = filesUnder(join(api, 'Portfolio.Tests'), (name) => name.endsWith('.cs'));
  if (files.length === 0) {
    problems.push('no .NET test files found');
    return 0;
  }

  let total = 0;
  for (const file of files) {
    const text = readFileSync(file, 'utf8');
    total += countMatches(text, /\[(?:RequiresDocker)?Fact(?:Attribute)?\]/g);
    total += countMatches(text, /\[InlineData\(/g);
  }
  return total;
}

/** Vitest reports one case per `it(`. `describe` blocks are grouping, not tests. */
function countFrontendTests() {
  const dir = join(root, 'src', 'frontend', 'portfolio-web', 'src');
  const files = filesUnder(dir, (name) => name.endsWith('.spec.ts'));
  if (files.length === 0) {
    problems.push('no frontend test files found');
    return 0;
  }

  let total = 0;
  for (const file of files) {
    total += countMatches(readFileSync(file, 'utf8'), /^\s*it\(/gm);
  }
  return total;
}

// --- schema ------------------------------------------------------------------------------------
function countTables() {
  const migrations = filesUnder(
    join(api, 'Portfolio.Infrastructure', 'Database', 'Migrations'),
    (name) => name.endsWith('.cs') && !name.endsWith('.Designer.cs') && !name.includes('ModelSnapshot'),
  );

  if (migrations.length === 0) {
    problems.push('no migrations found');
    return 0;
  }

  // Tables created minus tables dropped, across every migration in order, so the count stays right
  // after a future migration renames or removes one.
  const created = new Set();
  for (const file of migrations.sort()) {
    const text = readFileSync(file, 'utf8');
    const up = text.split('protected override void Down')[0] ?? text;
    for (const match of up.matchAll(/CreateTable\(\s*name:\s*"([a-z_]+)"/g)) created.add(match[1]);
    for (const match of up.matchAll(/DropTable\(\s*name:\s*"([a-z_]+)"/g)) created.delete(match[1]);
  }
  return created.size;
}

// --- API ---------------------------------------------------------------------------------------
function countEndpoints() {
  const text = read(join(api, 'Portfolio.Api', 'Endpoints', 'PortfolioEndpoints.cs'));
  if (text === null) {
    problems.push('endpoint file not found');
    return 0;
  }
  const count = countMatches(text, /api\.Map(Get|Post|Put|Delete|Patch)\(/g);
  if (count === 0) problems.push('no endpoints matched');
  return count;
}

// --- decisions ---------------------------------------------------------------------------------
function countAdrs() {
  const dir = join(root, 'docs', 'adr');
  const files = existsSync(dir) ? readdirSync(dir).filter((f) => /^\d{4}-.*\.md$/.test(f)) : [];
  if (files.length === 0) problems.push('no ADRs found');

  // Only accepted ones are claimed. A proposed or superseded record is not a decision in force.
  return files.filter((f) => /\*\*Status:\*\*\s*Accepted/i.test(readFileSync(join(dir, f), 'utf8'))).length;
}

// --- content -----------------------------------------------------------------------------------
function contentFacts() {
  const dir = join(root, 'content');
  const locales = readdirSync(dir)
    .map((f) => /^profile\.([a-z]{2})\.json$/.exec(f)?.[1])
    .filter(Boolean)
    .sort();

  const experience = JSON.parse(readFileSync(join(dir, 'experience.en.json'), 'utf8')).experience;
  const projects = JSON.parse(readFileSync(join(dir, 'projects.en.json'), 'utf8')).projects;

  if (locales.length === 0) problems.push('no locales found in content/');

  return {
    languages: locales.length,
    locales,
    roles: experience.length,
    projects: projects.length,
    publiclySourcedProjects: projects.filter((p) => p.publiclySourced).length,
  };
}

// --- write -------------------------------------------------------------------------------------
const content = contentFacts();
const facts = {
  $comment: 'GENERATED by tools/engineering/collect-facts.mjs. Do not edit — see docs/adr/0005-engineering-section.md.',
  generatedAt: new Date().toISOString().slice(0, 10),
  tests: countDotnetTests() + countFrontendTests(),
  backendTests: countDotnetTests(),
  frontendTests: countFrontendTests(),
  tables: countTables(),
  endpoints: countEndpoints(),
  adrs: countAdrs(),
  ...content,
};

if (problems.length > 0) {
  for (const p of problems) console.error(`  FAIL  ${p}`);
  console.error('\nA fact that cannot be measured must be removed from the page, not hardcoded.');
  process.exitCode = 1;
} else {
  writeFileSync(join(root, 'content', 'engineering-facts.json'), `${JSON.stringify(facts, null, 2)}\n`, 'utf8');
  for (const [key, value] of Object.entries(facts)) {
    if (!key.startsWith('$') && key !== 'generatedAt') console.log(`  ${key.padEnd(18)} ${Array.isArray(value) ? value.join(', ') : value}`);
  }
  console.log('\nWrote content/engineering-facts.json');
}
