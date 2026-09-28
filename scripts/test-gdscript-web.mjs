// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
// npm install --prefix artifacts/browser-test --no-audit --no-fund playwright-core@1.56.1
import { chromium } from '../artifacts/browser-test/node_modules/playwright-core/index.mjs';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import { fileURLToPath } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
// Pass gdscript-web-threads to test the thread-enabled export.
const name = process.argv[2] ?? 'gdscript-web';
const directory = path.join(root, 'artifacts', name);
const log = [];
const mime = { '.html': 'text/html', '.js': 'text/javascript', '.wasm': 'application/wasm', '.png': 'image/png' };
const server = http.createServer((request, response) => {
  const url = new URL(request.url, 'http://localhost');
  const file = path.resolve(directory, '.' + decodeURIComponent(url.pathname === '/' ? '/index.html' : url.pathname));
  if (!file.startsWith(directory + path.sep) || !fs.existsSync(file)) { response.writeHead(404).end(); return; }
  response.writeHead(200, { 'Content-Type': mime[path.extname(file)] ?? 'application/octet-stream',
    'Cross-Origin-Opener-Policy': 'same-origin', 'Cross-Origin-Embedder-Policy': 'require-corp' });
  fs.createReadStream(file).pipe(response);
});
await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
let browser;
try {
  browser = await chromium.launch({ channel: process.env.GDSCRIPT_BROWSER ?? 'msedge', headless: true,
    args: ['--enable-unsafe-swiftshader'] });
  const page = await browser.newPage();
  page.on('console', message => log.push(`${message.type()}: ${message.text()}`));
  page.on('pageerror', error => log.push(`pageerror: ${error.message}`));
  await page.goto(`http://127.0.0.1:${server.address().port}/`);
  await page.waitForFunction(() => window.tweensTestResult, { }, { timeout: 60000 });
  const result = await page.evaluate(() => window.tweensTestResult);
  fs.writeFileSync(path.join(root, `artifacts/${name}-result.json`), JSON.stringify({ browser: browser.version(), ...result }, null, 2));
  console.log(`Web/WASM (${name}): ${result.checks} checks, ${result.failures.length} failures (${browser.version()}).`);
  if (result.failures.length) throw new Error(result.failures.join('\n'));
} finally {
  fs.writeFileSync(path.join(root, `artifacts/${name}.log`), log.join('\n'));
  await browser?.close();
  await new Promise(resolve => server.close(resolve));
}
