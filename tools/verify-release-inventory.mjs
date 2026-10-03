// Validate the declared release inventory against actual files without modifying content.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../RelaxKonServer/Content');
const delivery = path.join(root, 'ReleaseDelivery');
const entries = JSON.parse(fs.readFileSync(path.join(root, 'Downloads/downloads.json'), 'utf8'));
let problems = 0;
for (const item of entries.filter(item => item.isAvailable)) {
  const artifactPath = decodeURIComponent(new URL(item.url, 'https://relaxkon.com').pathname);
  const resolved = path.resolve(delivery, artifactPath.replace(/^\//, ''));
  const relative = path.relative(delivery, resolved);
  if (relative.startsWith('..') || path.isAbsolute(relative) || !fs.existsSync(resolved)) {
    console.error('Missing or unsafe artifact:', item.fileName); problems++; continue;
  }
  const hash = crypto.createHash('sha256');
  for await (const chunk of fs.createReadStream(resolved)) hash.update(chunk);
  if (hash.digest('hex') !== item.checksum) { console.error('Checksum mismatch:', item.fileName); problems++; }
  const releaseFile = path.join(root, 'Releases', item.version + '.md');
  if (!fs.existsSync(releaseFile)) { console.error('Missing release record:', item.version); problems++; }
  else if (!fs.readFileSync(releaseFile, 'utf8').includes(item.fileName)) { console.error('Artifact absent from release record:', item.fileName); problems++; }
  console.log('Checked', item.fileName);
}
console.log(problems ? 'FAILED: ' + problems + ' problem(s)' : 'OK: available artifacts match checksums and release records');
process.exitCode = problems ? 1 : 0;
