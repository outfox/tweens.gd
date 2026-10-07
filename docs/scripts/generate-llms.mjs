import { readdir, readFile, writeFile } from 'node:fs/promises';
import { join, relative, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { load } from 'cheerio';
import TurndownService from 'turndown';
import { tables, strikethrough } from 'turndown-plugin-gfm';
import { LANGS, PATH, SHARED, pagesOf } from '../src/tracks.mjs';

const markdown = new TurndownService({
  headingStyle: 'atx',
  codeBlockStyle: 'fenced',
  bulletListMarker: '-',
});
markdown.use([tables, strikethrough]);
markdown.addRule('codeBlocks', {
  filter: 'pre',
  replacement: (_content, node) => {
    const code = node.textContent;
    const longest = Math.max(2, ...[...code.matchAll(/`+/g)].map(([run]) => run.length));
    const fence = '`'.repeat(longest + 1);
    return `\n\n${fence}${node.getAttribute('data-language') ?? ''}\n${code}${code.endsWith('\n') ? '' : '\n'}${fence}\n\n`;
  },
});
markdown.addRule('languages', {
  filter: (node) => node.hasAttribute('data-lang-only'),
  replacement: (content, node) =>
    `\n\n**${LANGS[node.getAttribute('data-lang-only')].label}:**\n\n${content.trim()}\n\n`,
});

async function htmlFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  return (
    await Promise.all(
      entries.map((entry) => {
        const path = join(directory, entry.name);
        return entry.isDirectory() ? htmlFiles(path) : path.endsWith('.html') ? [path] : [];
      }),
    )
  ).flat();
}

/** Convert rendered content so MDX components and generated catalog tables are included. */
export function extractPage(html, url) {
  const $ = load(html);
  const content = $('main .sl-markdown-content');
  if (!content.length || /\bnoindex\b/i.test($('meta[name="robots"]').attr('content') ?? '')) {
    return undefined;
  }
  const heading = $('main h1').first().clone();
  heading.find('[aria-hidden="true"]').remove();
  const title = heading.text().trim() || heading.find('img[alt]').attr('alt');
  const description = $('meta[name="description"]').attr('content')?.trim() ?? '';

  // Code highlighting wraps each line in divs, without newline text nodes.
  content.find('pre').each((_index, element) => {
    const pre = $(element);
    const lines = pre.find('.ec-line');
    const copy = pre.closest('.expressive-code').find('[data-code]').attr('data-code');
    const code =
      copy !== undefined
        ? copy.replace(/\u007f/g, '\n')
        : lines.length
          ? lines
              .map((_line, line) => $(line).text().replace(/\n$/, ''))
              .get()
              .join('\n')
          : pre.text();
    pre.empty().text(code);
  });
  content.find('.expressive-code .header').each((_index, element) => {
    $(element).replaceWith($('<p>').text($(element).find('.title').text()));
  });
  content.find('script, style, svg, button, input, select, .sl-anchor-link').remove();
  // Keep textual component content and both languages, even when CSS hides a variant.
  // Interactive stages need a browser; the surrounding prose and examples remain useful.
  content.find('[data-motion-demo]').replaceWith($('<p>').text(`Interactive preview: ${url}`));
  content.find('[data-lang-switch], .sr-only').remove();
  content.find('a[href], img[src]').each((_index, element) => {
    const attribute = element.tagName === 'a' ? 'href' : 'src';
    const node = $(element);
    node.attr(attribute, new URL(node.attr(attribute), url).href);
  });
  const body = markdown.turndown(content.html()).trim();
  if (!title || !body) throw new Error(`Missing documentation title or content: ${url}`);
  // Full-document groups use h2 and pages use h3; shift only real content headings.
  content.find('h1, h2, h3, h4, h5, h6').each((_index, element) => {
    const level = Math.min(6, Number(element.tagName.slice(1)) + 2);
    $(element).replaceWith($(`<h${level}>`).html($(element).html()));
  });
  const fullBody = markdown.turndown(content.html()).trim();
  return { title, description, body, fullBody };
}

function sectionsFor(pages) {
  const remaining = new Map(pages);
  const sections = [];
  const add = (title, routes) => {
    const entries = routes.map((route) => {
      const page = remaining.get(route);
      if (!page) throw new Error(`Documentation navigation has no exported page: ${route}`);
      remaining.delete(route);
      return page;
    });
    sections.push({ title, entries });
  };
  add('Overview and shared guides', [
    '/',
    ...new Set(
      PATH.flatMap(pagesOf)
        .map((p) => p.link)
        .filter(Boolean),
    ),
  ]);
  for (const [lang, { label }] of Object.entries(LANGS)) {
    for (const group of PATH) {
      add(
        `${label}: ${group.label}`,
        pagesOf(group)
          .filter((p) => p.slug)
          .map((p) => `/${lang}/${p.slug}/`),
      );
    }
  }
  add(
    'Project',
    SHARED.filter((p) => p.slug !== 'acknowledgements').map((p) => `/${p.slug}/`),
  );
  if (remaining.size) {
    sections.push({
      title: 'Optional',
      entries: [...remaining.values()].sort((a, b) => a.url.localeCompare(b.url)),
    });
  }
  return sections;
}

/** Runs on every Astro build, including the hosting provider's normal astro build command. */
export default function llmsDocumentation() {
  let site;
  return {
    name: 'tweens-llms-documentation',
    hooks: {
      'astro:config:done': ({ config }) => {
        site = config.site;
      },
      'astro:build:done': async ({ dir, logger }) => {
        if (!site) throw new Error('LLM documentation requires the Astro site URL.');
        const root = fileURLToPath(dir);
        const pages = new Map();
        for (const file of (await htmlFiles(root)).sort()) {
          const path = '/' + relative(root, file).split(sep).join('/');
          if (path === '/404.html') continue;
          const route = path.replace(/index\.html$/, '');
          const url = new URL(route, site).href;
          const html = await readFile(file, 'utf8');
          const content = extractPage(html, url);
          if (!content) continue; // Redirects have no documentation body.
          const markdownPath = path.replace(/\.html$/, '.md');
          const markdownUrl = new URL(markdownPath, site).href;
          const lang = LANGS[route.split('/')[1]]?.label;
          const title = lang ? `${lang}: ${content.title}` : content.title;
          const document = `# ${title}\n\nSource: ${url}\n\n${content.body}\n`;
          await writeFile(join(root, markdownPath.slice(1)), document);
          const discovery = `<link rel="alternate" type="text/markdown" href="${markdownPath}"><link rel="describedby" href="/llms.txt">`;
          await writeFile(file, html.replace('</head>', discovery + '</head>'));
          pages.set(route, { ...content, title, url, markdownUrl });
        }
        const sections = sectionsFor(pages);
        const introduction = `# tweens.gd\n\n> ${pages.get('/').description}\n\nChoose the C# or GDScript track to match your project. Shared guides cover both languages; language-specific examples are labeled. Read installation and compatibility before using the API.\n\nThese files are generated from the published documentation, including rendered examples and catalog tables. Interactive previews are available on the source pages.\n`;
        const index =
          introduction +
          `\n## Complete documentation\n\n- [llms-full.txt](${new URL('/llms-full.txt', site).href}): All documentation in one Markdown text file.\n` +
          sections
            .map(
              ({ title, entries }) =>
                `\n## ${title}\n\n` +
                entries
                  .map(
                    (page) =>
                      `- [${page.title.replace(/[\[\]]/g, '')}](${page.markdownUrl})${page.description ? ': ' + page.description : ''}\n`,
                  )
                  .join(''),
            )
            .join('');
        const full =
          introduction +
          sections
            .map(
              ({ title, entries }) =>
                `\n## ${title}\n` +
                entries
                  .map(
                    (page) =>
                      `\n---\n\n### ${page.title}\n\nSource: ${page.url}\n\n${page.fullBody}\n`,
                  )
                  .join(''),
            )
            .join('');
        await Promise.all([
          writeFile(join(root, 'llms.txt'), index),
          writeFile(join(root, 'llms-full.txt'), full),
        ]);
        logger.info(`Generated llms.txt, llms-full.txt and ${pages.size} Markdown pages.`);
      },
    },
  };
}
