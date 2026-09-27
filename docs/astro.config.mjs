// @ts-check
import { readdir, readFile, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import { tweensDark, tweensLight } from './src/styles/code-themes.mjs';
import { redirects, sidebar } from './src/tracks.mjs';

const elements = (node, tagName) => (node?.children ?? []).filter((c) => c.type === 'element' && c.tagName === tagName);

// Wraps Markdown tables in a scroll container, so a wide table scrolls inside the content lane instead of spilling past it.
// Each body cell also gets its column header as data-label, which narrow screens show when they stack rows.
// Registered on the Sätteri processor's hast pipeline, the same way Starlight adds its own transforms.
const tableScroll = {
	name: 'tweens-table-scroll',
	hooks: {
		'astro:config:setup': ({ config }) =>
			config.markdown.processor.options.hastPlugins.push({
				name: 'tweens-table-wrap',
				element: [
					{
						filter: ['table'],
						visit: (node, ctx) => {
							const headers = elements(elements(elements(node, 'thead')[0], 'tr')[0], 'th').map((th) =>
								ctx.textContent(th).trim(),
							);
							for (const tr of elements(elements(node, 'tbody')[0], 'tr'))
								elements(tr, 'td').forEach((td, i) => headers[i] && ctx.setProperty(td, 'dataLabel', headers[i]));
							ctx.wrapNode(node, { type: 'element', tagName: 'div', properties: { className: ['table-wrap'] }, children: [] });
						},
					},
				],
			}),
	},
};

// Astro doesn't preload the modules a page's scripts import, so a first visit found them only once the script had
// arrived, and every script after it waited that extra round trip. This links each static import up front;
// dynamic import() stays lazy.
const preloadImports = {
	name: 'tweens-modulepreload',
	hooks: {
		'astro:build:done': async ({ dir }) => {
			const root = fileURLToPath(dir);
			const cache = new Map();
			/** @param {string} href @returns {Promise<string[]>} */
			const importsOf = async (href) => {
				if (!cache.has(href)) {
					const code = await readFile(join(root, href), 'utf8');
					const specs = [...code.matchAll(/\bimport\s*(?:[\w$*{}\s,]+from\s*)?["'](\.\.?\/[^"']+)["']/g)];
					cache.set(href, specs.map((m) => new URL(m[1], `https://x${href}`).pathname));
				}
				return cache.get(href);
			};
			const pages = (await readdir(root, { recursive: true })).filter((f) => f.endsWith('.html'));
			for (const page of pages) {
				const file = join(root, page);
				const html = await readFile(file, 'utf8');
				const scripts = [...html.matchAll(/<script type="module" src="([^"]+)"/g)].map((m) => m[1]);
				const found = new Set();
				/** @param {string} href */
				const visit = async (href) => {
					for (const dep of await importsOf(href)) {
						if (found.has(dep)) continue;
						found.add(dep);
						await visit(dep);
					}
				};
				for (const src of scripts) await visit(src);
				for (const src of scripts) found.delete(src);
				const links = [...found].map((href) => `<link rel="modulepreload" href="${href}">`).join('');
				if (links) await writeFile(file, html.replace('</head>', `${links}</head>`));
			}
		},
	},
};

// https://astro.build/config
export default defineConfig({
	site: 'https://tweens.gd',
	redirects,
	// Starlight turns on Astro's link prefetch by default. Firefox can't reuse those prefetches for pages served
	// with max-age=0, so each one doubled the request and could hold up the click. Head.astro uses Speculation
	// Rules instead.
	prefetch: false,
	vite: {
		build: {
			// A demo's own small stylesheet goes inline, so a first visit doesn't wait on a render-blocking request
			// for it. Other assets keep Vite's default.
			assetsInlineLimit: (file, content) => (file.endsWith('.css') ? content.length < 10 * 1024 : undefined),
		},
	},
	integrations: [
		tableScroll,
		preloadImports,
		starlight({
			title: 'tweens.gd',
			description: "A tweening library for Godot (that doesn't suck.)",
			tableOfContents: false,
			customCss: [
				'@fontsource-variable/bricolage-grotesque/standard.css',
				'@fontsource-variable/figtree',
				'@fontsource-variable/jetbrains-mono',
				'./src/styles/theme.css',
			],
			components: {
				Head: './src/components/overrides/Head.astro',
				Hero: './src/components/overrides/Hero.astro',
				PageTitle: './src/components/overrides/PageTitle.astro',
				SiteTitle: './src/components/overrides/SiteTitle.astro',
				MarkdownContent: './src/components/overrides/MarkdownContent.astro',
				Header: './src/components/overrides/Header.astro',
				Sidebar: './src/components/overrides/Sidebar.astro',
			},
			expressiveCode: {
				themes: [tweensDark, tweensLight],
				styleOverrides: {
					borderRadius: '0.9rem',
					borderColor: 'var(--tw-outline)',
					codeFontFamily: 'var(--__sl-font-mono)',
					uiFontFamily: 'var(--__sl-font)',
					codeBackground: 'var(--tw-stage)',
					frames: {
						editorTabBarBackground: 'var(--tw-surface)',
						// The theme's accent stripe on the active tab gets clipped by the frame's corner radius.
						editorActiveTabIndicatorTopColor: 'transparent',
						editorActiveTabIndicatorBottomColor: 'transparent',
						terminalTitlebarBackground: 'var(--tw-surface)',
						frameBoxShadowCssValue: 'var(--tw-shadow)',
					},
				},
			},
			social: [
				{ icon: 'discord', label: 'Discord', href: 'https://discord.gg/3UXVHnmEwd' },
				{ icon: 'github', label: 'GitHub', href: 'https://github.com/outfox/tweens.gd' },
			],
			// One learning path per language (src/tracks.mjs); route middleware shows only the current page's path.
			sidebar: sidebar(),
			routeMiddleware: './src/routeData.ts',
		}),
	],
});
