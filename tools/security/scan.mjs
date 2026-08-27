/**
 * A baseline secret and privacy check over the tracked files.
 *
 * This is not a replacement for a real scanner. It is the small set of mistakes this repository can
 * actually make — a personal phone number, the untracked CV, a connection string with a password
 * that is not the local throwaway one — turned into a build failure so they cannot be committed
 * quietly. Dependency-free on purpose: a security check that pulls a third-party action to run is
 * new supply-chain surface for the thing it is meant to protect.
 *
 *   node tools/security/scan.mjs
 *
 * Exits non-zero on any finding.
 */

import { execFileSync } from 'node:child_process';
import { readFileSync, statSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..');

const findings = [];
const report = (file, line, what) => findings.push({ file, line, what });

/** Only tracked files: anything git ignores cannot be leaked by a push. */
function trackedFiles() {
  return execFileSync('git', ['ls-files'], { cwd: root, encoding: 'utf8' })
    .split('\n')
    .filter(Boolean);
}

// Local development credentials are deliberately boring and deliberately committed; flagging them
// would train everyone to ignore this tool. Anything else that looks like a credential is a finding.
const LOCAL_ONLY = /portfolio:portfolio|Username=portfolio;Password=portfolio|POSTGRES_PASSWORD: portfolio/;

const PATTERNS = [
  { what: 'private key block', re: /-----BEGIN (?:RSA |EC |OPENSSH |PGP )?PRIVATE KEY-----/ },
  { what: 'AWS access key id', re: /\bAKIA[0-9A-Z]{16}\b/ },
  { what: 'GitHub token', re: /\bgh[pousr]_[A-Za-z0-9]{36,}\b/ },
  { what: 'Slack token', re: /\bxox[abprs]-[A-Za-z0-9-]{10,}\b/ },
  { what: 'Azure storage key', re: /AccountKey=[A-Za-z0-9+/=]{40,}/ },
  { what: 'bearer token', re: /\b[Bb]earer\s+[A-Za-z0-9._~+/-]{30,}={0,2}/ },
  // Angle brackets are excluded rather than the rule relaxed. `Password=<password>` in a deployment
  // guide is documentation; no real credential contains `<` or `>`, so the placeholder form is safe
  // to skip while a genuine value of the same shape still fails. Narrowed after this pattern caught
  // an example in docs/deployment.md — which is the tool working, not a reason to weaken it.
  { what: 'password in a connection string', re: /(?:Password|Pwd)\s*=\s*(?!portfolio\b)(?!<)[^\s;"'<>]{6,}/i },
  { what: 'Colombian phone number', re: /\+?\s*\(?\+?57\)?[\s.-]*3\d{2}[\s.-]*\d{3}[\s.-]*\d{4}/ },
];

// Files that must never be tracked at all, regardless of content.
const FORBIDDEN_PATHS = [
  { what: 'the original CV, which carries a phone number', re: /Sebastian_Velez_CV_Updated\.pdf$/ },
  { what: 'untracked personal data directory', re: /^content\/private\// },
  // The redacted CVs and the preview images are tracked on purpose so a git-connected host can
  // build the site. What must never be tracked is the OTHER variant: `_full` is the one that
  // carries the phone number, and copying the wrong file into public/cv is the realistic mistake
  // here, not committing the directory at all.
  { what: 'the CV variant that carries a phone number', re: /_full\.pdf$/i },
  { what: 'a local settings override', re: /appsettings\.[^.]*\.?Local\.json$/i },
  { what: 'an environment file', re: /(^|\/)\.env($|\.(?!example))/ },
];

const BINARY = /\.(pdf|png|jpe?g|gif|ico|woff2?|ttf|eot|otf|zip|dll|exe|webp|avif)$/i;

// This file lists the patterns it looks for, so scanning it finds all of them.
const SELF = 'tools/security/scan.mjs';

for (const file of trackedFiles()) {
  for (const rule of FORBIDDEN_PATHS) {
    if (rule.re.test(file)) report(file, 0, `must not be tracked: ${rule.what}`);
  }

  if (file === SELF || BINARY.test(file)) continue;

  let text;
  try {
    if (statSync(join(root, file)).size > 2_000_000) continue;
    text = readFileSync(join(root, file), 'utf8');
  } catch {
    continue;
  }

  const lines = text.split('\n');
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (LOCAL_ONLY.test(line)) continue;

    for (const rule of PATTERNS) {
      if (rule.re.test(line)) report(file, i + 1, rule.what);
    }
  }
}

if (findings.length > 0) {
  for (const f of findings) {
    console.error(`  FAIL  ${f.file}${f.line ? `:${f.line}` : ''} — ${f.what}`);
  }
  console.error(`\n${findings.length} finding(s). Nothing here is a false positive worth ignoring:`);
  console.error('if a match is genuinely safe, narrow the pattern rather than deleting the check.');
  process.exitCode = 1;
} else {
  console.log(`  ok    scanned ${trackedFiles().length} tracked files, no findings.`);
}
