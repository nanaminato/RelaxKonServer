// Verify document front matter: per-language file counts, duplicate `order`
// values, and the resolution order that drives prev/next navigation.
//
// Usage: node tools/verify-doc-order.mjs [DocsRoot]
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Default to this repository's own content tree so the script works from any
// working directory: <repo>/tools/ -> <repo>/RelaxKonServer/Content/Docs.
const defaultRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', 'RelaxKonServer', 'Content', 'Docs');
const root = path.resolve(process.argv[2] ?? defaultRoot);
const LANGS = ['zh-CN', 'en-US', 'ja-JP'];
const CATEGORIES = ['getting-started', 'concepts', 'apps'];

const readFrontMatter = (file) => {
  const text = fs.readFileSync(file, 'utf8');
  const block = /^---\r?\n([\s\S]*?)\r?\n---/.exec(text);
  const fm = block ? block[1] : '';
  const get = (key) => {
    const m = new RegExp(`^${key}:\\s*(.*)$`, 'm').exec(fm);
    return m ? m[1].trim().replace(/^["']|["']$/g, '') : '';
  };
  return { title: get('title'), order: Number.parseInt(get('order'), 10) || 0, category: get('category') };
};

let problems = 0;
const inventory = {};

for (const lang of LANGS) {
  inventory[lang] = {};
  for (const category of CATEGORIES) {
    const dir = path.join(root, lang, 'latest', category);
    if (!fs.existsSync(dir)) {
      console.log(`MISSING DIR ${lang}/latest/${category}`);
      problems += 1;
      inventory[lang][category] = [];
      continue;
    }
    const entries = fs
      .readdirSync(dir)
      .filter((name) => name.endsWith('.md'))
      .map((name) => {
        const slug = `${category}/${name.slice(0, -3)}`;
        return { slug, file: name, ...readFrontMatter(path.join(dir, name)) };
      })
      .sort((a, b) => a.order - b.order || a.slug.localeCompare(b.slug));

    inventory[lang][category] = entries;

    const seen = new Map();
    for (const entry of entries) {
      const key = entry.order;
      if (seen.has(key)) {
        console.log(`DUPLICATE ORDER ${lang}/${category}: ${seen.get(key)} and ${entry.slug} both order=${key}`);
        problems += 1;
      } else {
        seen.set(key, entry.slug);
      }
      if (!entry.title) {
        console.log(`MISSING TITLE ${lang}/${entry.slug}`);
        problems += 1;
      }
    }
  }
}

// `order` must be globally unique across the whole document set, not merely
// unique inside a category: DocumentService.GetDocumentAsync sorts every
// document by Order alone when it derives the prev/next links, so a collision
// silently makes those links jump to an arbitrary sibling.
for (const lang of LANGS) {
  const all = CATEGORIES.flatMap((category) => inventory[lang][category]);
  const seen = new Map();
  for (const entry of [...all].sort((a, b) => a.order - b.order)) {
    if (seen.has(entry.order)) {
      console.log(`GLOBAL DUPLICATE ORDER ${lang}: ${seen.get(entry.order)} and ${entry.slug} both order=${entry.order}`);
      problems += 1;
    } else {
      seen.set(entry.order, entry.slug);
    }
  }
}

// The sidebar orders categories by the smallest Order inside each group, so the
// apps block must start below the concepts block or the two swap places.
for (const lang of LANGS) {
  const minOf = (category) => Math.min(...inventory[lang][category].map((e) => e.order));
  if (minOf('apps') >= minOf('concepts')) {
    console.log(`CATEGORY ORDER ${lang}: apps min=${minOf('apps')} is not below concepts min=${minOf('concepts')}; sidebar will show concepts first`);
    problems += 1;
  }
}

// Cross-language parity: identical slug sets, else the sidebar falls back to English.
const base = LANGS[0];
for (const lang of LANGS.slice(1)) {
  for (const category of CATEGORIES) {
    const a = new Set(inventory[base][category].map((e) => e.slug));
    const b = new Set(inventory[lang][category].map((e) => e.slug));
    const onlyBase = [...a].filter((s) => !b.has(s));
    const onlyLang = [...b].filter((s) => !a.has(s));
    if (onlyBase.length || onlyLang.length) {
      console.log(`PARITY ${lang}/${category}: missing=[${onlyBase}] extra=[${onlyLang}]`);
      problems += 1;
    }
  }
}

for (const lang of LANGS) {
  const counts = CATEGORIES.map((c) => `${c}=${inventory[lang][c].length}`).join(' ');
  const total = CATEGORIES.reduce((sum, c) => sum + inventory[lang][c].length, 0);
  console.log(`${lang}: ${counts}  total=${total}`);
}

console.log('');
console.log('resolution order (drives prev/next navigation):');
for (const category of CATEGORIES) {
  console.log(`  ${category}`);
  for (const entry of inventory[base][category]) {
    console.log(`    ${String(entry.order).padStart(3)}  ${entry.slug.padEnd(34)} ${entry.title}`);
  }
}

console.log('');
console.log(problems === 0 ? 'OK: no problems found' : `FAILED: ${problems} problem(s)`);
process.exitCode = problems === 0 ? 0 : 1;
