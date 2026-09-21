// Check every internal /docs link and every front matter block in the content
// tree: dead targets, cross-language gaps, and missing required keys.
//
// Usage: node tools/verify-doc-links.mjs [DocsRoot]
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Default to this repository's own content tree so the script works from any
// working directory: <repo>/tools/ -> <repo>/RelaxKonServer/Content/Docs.
const defaultRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', 'RelaxKonServer', 'Content', 'Docs');
const root = path.resolve(process.argv[2] ?? defaultRoot);

const langs = ['en-US', 'zh-CN', 'ja-JP'];

function walk(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) return walk(p);
    return e.name.endsWith('.md') ? [p] : [];
  });
}

// slug set per language, e.g. "apps/docker"
const slugsByLang = {};
for (const lang of langs) {
  const base = path.join(root, lang, 'latest');
  slugsByLang[lang] = new Set(
    walk(base).map(f => path.relative(base, f).replace(/\\/g, '/').slice(0, -3)),
  );
}

// union of all slugs (a page may exist in one language only; the server falls back to en-US)
const allSlugs = new Set(langs.flatMap(l => [...slugsByLang[l]]));

const linkRe = /\]\((\/docs\/([A-Za-z0-9-]+)\/([A-Za-z0-9._-]+)\/([^)\s#]+))/g;
const problems = [];
let checked = 0;

for (const lang of langs) {
  const base = path.join(root, lang, 'latest');
  for (const file of walk(base)) {
    const rel = path.relative(root, file).replace(/\\/g, '/');
    const text = fs.readFileSync(file, 'utf8');
    for (const m of text.matchAll(linkRe)) {
      const [, , linkLang, linkVersion, slug] = m;
      checked++;
      if (!langs.includes(linkLang)) {
        problems.push(`${rel}: unknown language "${linkLang}" in ${m[1]}`);
        continue;
      }
      if (linkVersion !== 'latest') {
        problems.push(`${rel}: non-latest version "${linkVersion}" in ${m[1]}`);
        continue;
      }
      if (!allSlugs.has(slug)) {
        problems.push(`${rel}: dead link -> ${m[1]}`);
        continue;
      }
      if (!slugsByLang[linkLang].has(slug)) {
        problems.push(`${rel}: cross-language link to a page missing in ${linkLang} -> ${m[1]}`);
      }
    }
    // front matter sanity
    if (!/^---\r?\n/.test(text)) problems.push(`${rel}: missing front matter`);
    for (const key of ['title', 'description', 'category', 'order']) {
      if (!new RegExp(`^${key}:`, 'm').test(text.split('---')[1] ?? '')) {
        problems.push(`${rel}: front matter missing "${key}"`);
      }
    }
  }
}

console.log(`languages: ${langs.map(l => `${l}=${slugsByLang[l].size}`).join(' ')}  (union ${allSlugs.size})`);
console.log(`internal /docs links checked: ${checked}`);
if (problems.length === 0) {
  console.log('OK: no broken links, no cross-language gaps, all front matter complete.');
} else {
  console.log(`PROBLEMS (${problems.length}):`);
  for (const p of problems) console.log('  - ' + p);
  process.exitCode = 1;
}
