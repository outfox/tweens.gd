// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
// npm install --prefix artifacts/browser-test --no-audit --no-fund playwright-core@1.56.1
// node scripts/test-tutorial-web.mjs <AppBundle directory> [artifact name]
import { chromium } from '../artifacts/browser-test/node_modules/playwright-core/index.mjs';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const directory = path.resolve(
  process.argv[2] ?? path.join(root, 'tutorial/tutorial.web/AppBundle'),
);
const name = process.argv[3] ?? 'tutorial-web';
const logPath = path.join(root, 'artifacts', `${name}.log`);
fs.mkdirSync(path.dirname(logPath), { recursive: true });
const log = [];
const failures = [];
const mime = {
  '.html': 'text/html',
  '.js': 'text/javascript',
  '.wasm': 'application/wasm',
  '.json': 'application/json',
};
const server = http.createServer((request, response) => {
  const url = new URL(request.url, 'http://localhost');
  const file = path.resolve(
    directory,
    '.' + decodeURIComponent(url.pathname === '/' ? '/index.html' : url.pathname),
  );
  if (
    !file.startsWith(directory + path.sep) ||
    !fs.existsSync(file) ||
    !fs.statSync(file).isFile()
  ) {
    response.writeHead(404).end();
    return;
  }
  response.writeHead(200, {
    'Content-Type': mime[path.extname(file)] ?? 'application/octet-stream',
  });
  fs.createReadStream(file).pipe(response);
});
await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
let browser;
try {
  browser = await chromium.launch({
    channel: process.env.GDSCRIPT_BROWSER ?? 'msedge',
    headless: true,
    args: ['--enable-unsafe-swiftshader'],
  });
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  page.on('console', (message) => {
    log.push(`${message.type()}: ${message.text()}`);
    if (message.type() === 'error' || /SCRIPT ERROR|ERROR:/.test(message.text()))
      failures.push(message.text());
  });
  page.on('pageerror', (error) => {
    log.push(`pageerror: ${error.stack}`);
    failures.push(error.message);
  });
  await page.goto(`http://127.0.0.1:${server.address().port}/`);
  await page.waitForFunction(
    () =>
      !document.getElementById('status') || document.getElementById('status-notice')?.textContent,
    {},
    { timeout: 60000 },
  );
  assert.equal(
    await page.locator('#status-notice').count(),
    0,
    'The boot shell reported a failure.',
  );
  await page.waitForTimeout(500);

  for (const [language, x] of [
    ['csharp', 1100],
    ['gdscript', 1200],
  ]) {
    await page.mouse.click(x, 34);
    for (let step = 1; step <= 5; step++) {
      await page.mouse.click(548 + (step - 1) * 46, 34);
      await page.waitForTimeout(800);
      if (step === 2) {
        // Drive the native tween through the actual GDScript input handler, then verify
        // that moving between two targets changes the rendered stage above the code panel.
        const clip = { x: 200, y: 380, width: 550, height: 220 };
        await page.mouse.click(280, 420);
        await page.waitForTimeout(1800);
        const before = await page.screenshot({ clip });
        await page.mouse.click(700, 560);
        await page.waitForTimeout(1800);
        const after = await page.screenshot({ clip });
        assert.ok(!before.equals(after), `${language}: the icon did not move between clicks.`);
      }
      await page.screenshot({
        path: path.join(root, 'artifacts', `${name}-${language}-${step}.png`),
      });
    }
  }
  assert.deepEqual(failures, [], 'The tutorial logged browser or Godot errors.');
  console.log(
    `Tutorial Web/WASM: all five lessons loaded in both languages; C# and GDScript animations moved; no errors (${browser.version()}).`,
  );
} finally {
  fs.writeFileSync(logPath, log.join('\n'));
  await browser?.close();
  await new Promise((resolve) => server.close(resolve));
}
