import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { mkdtemp, mkdir, cp, readFile, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { resolve, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createServer } from 'node:net';

const project = fileURLToPath(new URL('../RelaxKonServer/', import.meta.url));
const temporary = await mkdtemp(join(tmpdir(), 'relaxkon-release-check-'));
let child;
try {
  const releases = join(temporary, 'Content', 'Releases');
  await mkdir(releases, { recursive: true });
  await cp(join(project, 'Content', 'Releases'), releases, { recursive: true });
  // A separate fixture exercises missing translations without changing real content.
  const shared = await readFile(join(releases, '0.1.2.md'), 'utf8');
  await writeFile(join(releases, 'fallback.md'), shared.replace('version: 0.1.2', 'version: fallback'));
  await cp(join(releases, 'en-US', '0.1.2.md'), join(releases, 'en-US', 'fallback.md'));
  const listener = createServer();
  await new Promise(resolve => listener.listen(0, '127.0.0.1', resolve));
  const port = listener.address().port;
  await new Promise(resolve => listener.close(resolve));
  const base = `http://127.0.0.1:${port}`;
  child = spawn('dotnet', [join(project, 'bin', 'Debug', 'net10.0', 'RelaxKonServer.dll'), '--contentRoot', temporary, '--urls', base], {
    cwd: temporary, windowsHide: true, stdio: 'ignore', env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development' },
  });
  for (let attempt = 0; ; attempt++) {
    try { if ((await fetch(base + '/api/health')).ok) break; } catch {}
    if (attempt === 60 || child.exitCode !== null) throw new Error('Test API did not start');
    await new Promise(resolve => setTimeout(resolve, 200));
  }
  const get = async path => {
    const response = await fetch(base + path);
    assert.equal(response.status, 200, path);
    return response.json();
  };
  const results = [];
  for (const [language, title] of [['zh-CN', '发布包清单'], ['ja-JP', '公開パッケージ一覧'], ['en-US', 'Published package inventory']]) {
    const list = await get('/api/releases?language=' + language);
    const detail = await get('/api/releases/0.1.2?language=' + language);
    assert.equal(detail.language, language);
    assert.equal(detail.isFallback, false);
    assert.ok(detail.title.endsWith(title));
    assert.equal(list.find(item => item.version === '0.1.2').title, detail.title);
    assert.equal(detail.releaseDate, '2026-09-16');
    assert.equal(detail.isPrerelease, false);
    assert.ok(!/\{(?:artifactsTitle|packageLabel|downloadsLabel)\}/.test(detail.content));
    assert.ok(!/## (简体中文|English|日本語)/.test(detail.content));
    results.push(detail);
    const fallback = await get('/api/releases/fallback?language=' + language);
    assert.equal(fallback.language, 'en-US');
    assert.equal(fallback.isFallback, language !== 'en-US');
    assert.equal(list.find(item => item.version === 'fallback').isFallback, language !== 'en-US');
  }
  const artifacts = content => content.match(/\[RelaxKonOS[^\n]+|\| RelaxKonOS[^\n]+/g);
  for (const result of results) assert.deepEqual(artifacts(result.content), artifacts(results[0].content));
  assert.equal((await get('/api/releases/0.1.2')).language, 'en-US');
  assert.equal((await get('/api/releases/0.1.2?language=unknown')).language, 'en-US');
  assert.equal((await fetch(base + '/api/releases/missing')).status, 404);
  console.log('OK: localized list/details, shared artifacts, English fallback, defaults and 404');
} finally {
  if (child && child.exitCode === null) {
    const exited = new Promise(resolve => child.once('exit', resolve));
    child.kill();
    await exited;
  }
  if (!resolve(temporary).startsWith(resolve(tmpdir()) + '\\') && !resolve(temporary).startsWith(resolve(tmpdir()) + '/'))
    throw new Error('Temporary cleanup escaped its root');
  await rm(temporary, { recursive: true, force: true });
}
