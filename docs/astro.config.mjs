// @ts-check
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

// https://astro.build/config
export default defineConfig({
	site: 'https://tweens.gd',
	redirects,
	integrations: [
		tableScroll,
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
