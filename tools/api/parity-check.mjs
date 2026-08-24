/**
 * Checks that the file content source and the database content source return the same thing.
 *
 * The API can read its content from content/ or from PostgreSQL. Those are two independent
 * implementations of the same contract, and the only way they stay honest is by comparing their
 * output rather than trusting that they agree. This caught a real bug: the two disagreed about the
 * order of a role's project list, which no unit test would have noticed.
 *
 * Start both APIs first, then run this:
 *
 *   dotnet run --project Portfolio.Api --urls http://localhost:5080
 *   Portfolio__Database__Enabled=true Portfolio__Database__ConnectionString="..." \
 *     dotnet run --project Portfolio.Api --urls http://localhost:5081
 *
 *   node tools/api/parity-check.mjs
 *
 * Exits non-zero on any difference, so CI can gate on it in phase 8.
 */

const fileBase = process.env.FILE_API ?? 'http://localhost:5080';
const dbBase = process.env.DB_API ?? 'http://localhost:5081';
const locales = ['en', 'es'];

/**
 * `language` is dropped before comparing: it reports how the language was negotiated for that
 * request, which is a property of the request and not of the content.
 */
async function fetchContent(base, lang) {
  const response = await fetch(`${base}/api/content?lang=${lang}`);
  if (!response.ok) {
    throw new Error(`${base} returned ${response.status} for lang=${lang}`);
  }
  const body = await response.json();
  delete body.language;
  return JSON.stringify(body, null, 1);
}

/** Reports the first differing line, which is enough to find the field that drifted. */
function firstDifference(a, b) {
  const left = a.split('\n');
  const right = b.split('\n');
  for (let i = 0; i < Math.max(left.length, right.length); i++) {
    if (left[i] !== right[i]) {
      return `line ${i + 1}\n  files: ${left[i] ?? '<end>'}\n  db:    ${right[i] ?? '<end>'}`;
    }
  }
  return 'lengths differ but no line differs';
}

let failures = 0;

for (const lang of locales) {
  try {
    const [fromFiles, fromDb] = await Promise.all([
      fetchContent(fileBase, lang),
      fetchContent(dbBase, lang),
    ]);

    if (fromFiles === fromDb) {
      console.log(`  ok    ${lang}: identical (${fromFiles.length} bytes)`);
    } else {
      failures++;
      console.error(`  FAIL  ${lang}: sources disagree at ${firstDifference(fromFiles, fromDb)}`);
    }
  } catch (error) {
    failures++;
    console.error(`  FAIL  ${lang}: ${error.message}`);
  }
}

console.log(`\n${failures} failure(s).`);
// exitCode rather than exit(): forcing exit while fetch keepalive handles are still closing trips an
// assertion in libuv on Windows.
process.exitCode = failures > 0 ? 1 : 0;
