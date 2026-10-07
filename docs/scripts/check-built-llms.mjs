import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';
import { load } from 'cheerio';
import { extractPage } from './generate-llms.mjs';

const output = new URL('../dist/', import.meta.url);
const readOutput = (path) => readFile(new URL(path, output), 'utf8');

test('Markdown preserves rendered code, tables, languages and absolute links', () => {
  const html = `<main>
    <h1><span class="sr-only">Example</span><span aria-hidden="true">Example</span></h1>
    <div class="sl-markdown-content">
      <h2>Usage</h2>
      <div data-lang-only="csharp"><p>C# example</p></div>
      <div data-lang-only="gdscript"><p>GDScript example</p></div>
      <div class="expressive-code"><div class="header"><span class="title">Example.cs</span></div>
        <pre data-language="csharp"><code><div class="ec-line">var x = 1;</div><div class="ec-line"></div><div class="ec-line">    Print(x);</div></code></pre>
        <button data-code="var x = 1;\u007f\u007f    Print(x);">Copy</button>
      </div>
      <pre data-language="text"><code># This is code, not a heading\n&#96;&#96;&#96;</code></pre>
      <table><thead><tr><th>Helper</th><th>Property</th></tr></thead>
        <tbody><tr><td><code>position_2d</code></td><td>position</td></tr></tbody></table>
      <p><a href="../installation/">Install</a> <a href="#usage">Usage</a></p>
      <script>unwanted_script()</script><button>unwanted_button</button>
    </div></main>`;
  const page = extractPage(html, 'https://tweens.gd/csharp/quickstart/');
  assert.equal(page.title, 'Example');
  assert.match(page.body, /```csharp\nvar x = 1;\n\n    Print\(x\);\n```/);
  assert.match(page.body, /````text\n# This is code, not a heading\n```\n````/);
  assert.match(page.body, /\| Helper \| Property \|/);
  assert.match(page.body, /\| `position_2d` \| position \|/);
  assert.match(page.body, /\*\*C#:\*\*/);
  assert.match(page.body, /\*\*GDScript:\*\*/);
  assert.match(page.body, /https:\/\/tweens.gd\/csharp\/installation\//);
  assert.match(page.body, /https:\/\/tweens.gd\/csharp\/quickstart\/#usage/);
  assert.doesNotMatch(page.body, /unwanted_/);
  assert.match(page.fullBody, /#### Usage/);
  assert.match(page.fullBody, /\n# This is code, not a heading\n/);
});

test('Redirects and noindex content are excluded', () => {
  assert.equal(
    extractPage('<meta http-equiv="refresh" content="0;url=/">', 'https://tweens.gd/old/'),
    undefined,
  );
  assert.equal(
    extractPage(
      '<meta name="robots" content="noindex"><main><div class="sl-markdown-content">Private</div></main>',
      'https://tweens.gd/private/',
    ),
    undefined,
  );
});

test('Built LLM files cover the sitemap and preserve published examples and catalogs', async () => {
  const index = await readOutput('llms.txt');
  const full = await readOutput('llms-full.txt');
  const robots = await readOutput('robots.txt');
  assert.match(index, /^# tweens\.gd\n\n> /);
  assert.match(index, /## C#: Learn/);
  assert.match(index, /## GDScript: Learn/);
  assert.match(index, /## Optional/);
  assert.doesNotMatch(index + full, /docs-internal|\/plans\/|\/artifacts\/|<script|<svg/);
  assert.match(robots, /User-agent: \*\nAllow: \/\n/);
  assert.match(robots, /Sitemap: https:\/\/tweens.gd\/sitemap-index\.xml/);
  assert.doesNotMatch(robots, /Disallow:|Crawl-delay:/i);

  const map = load(await readOutput('sitemap-index.xml'), { xml: true });
  const expected = [];
  for (const element of map('loc')) {
    const path = new URL(map(element).text()).pathname.slice(1);
    const child = load(await readOutput(path), { xml: true });
    expected.push(
      ...child('loc')
        .map((_index, loc) => child(loc).text())
        .get(),
    );
  }
  const links = [...index.matchAll(/^- \[[^\]]+\]\((https:[^)]+)\)/gm)].map((match) => match[1]);
  const sources = [...full.matchAll(/^Source: (https:\/\/\S+)$/gm)].map((match) => match[1]);
  assert.deepEqual(new Set(sources), new Set(expected));
  assert.equal(sources.length, expected.length, 'Each page appears once in llms-full.txt');
  assert.equal(
    links.length,
    expected.length + 1,
    'Each page and the full document have an index link',
  );
  for (const url of links) {
    const text = await readOutput(new URL(url).pathname.slice(1));
    assert.ok(text.startsWith('# '), `${url} is readable Markdown`);
    if (!url.endsWith('/index.md')) continue;
    const source = url.replace(/index\.md$/, '');
    assert.ok(expected.includes(source), `${url} points to a published page`);
    const html = await readOutput(new URL(source).pathname.slice(1) + 'index.html');
    const $ = load(html);
    assert.equal(
      $('link[rel="alternate"][type="text/markdown"]').attr('href'),
      new URL(url).pathname,
    );
    assert.equal($('link[rel="describedby"]').attr('href'), '/llms.txt');
    for (const element of $('.sl-markdown-content .expressive-code [data-code]')) {
      if ($(element).closest('[data-motion-demo]').length) continue;
      const code = $(element)
        .attr('data-code')
        .replace(/\u007f/g, '\n');
      assert.ok(text.includes(code), `${url} preserves the exact published code`);
    }
  }
  const catalog = await readOutput('gdscript/nodes/2d/index.md');
  assert.match(catalog, /\| Helper \| Property \| Value \|/);
  assert.match(catalog, /\| `position_2d` \| `position` \| Vector2 \|/);
});
