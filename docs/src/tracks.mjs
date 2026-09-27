// The site runs two parallel learning paths, one per language, with the same page slugs under /csharp/ and /gdscript/.
// astro.config.mjs builds the sidebar from this list; the route middleware, the language switch, and the section art
// read it too, so a page and its twin always sit at the same place in each path.

export const LANGS = {
	csharp: { label: 'C#', long: 'C#', status: 'Beta' },
	gdscript: { label: 'GDScript', long: 'GDScript', status: 'Beta' },
};

/** Groups in path order. `art` picks the drawing beside each page title; `gdscript` overrides a label there.
 *  An entry with `pages` instead of a `slug` is a nested sidebar group. */
export const PATH = [
	{
		label: 'Start here',
		art: 'dock',
		pages: [
			{ slug: 'installation', label: 'Install' },
			{ slug: 'quickstart', label: 'Your first tween' },
		],
	},
	{
		label: 'Write reusable tweens',
		art: 'hop',
		pages: [
			{ slug: 'definitions', label: 'Definitions' },
			{ slug: 'sequences', label: 'Sequences', gdscript: 'Groups & sequences' },
			{ slug: 'playback', label: 'Control & completion' },
			{ slug: 'cancellation', label: 'Cancellation & reasons' },
		],
	},
	{
		label: 'Shape the motion',
		art: 'curve',
		pages: [
			{ slug: 'easing', label: 'Easing' },
			{ slug: 'timing', label: 'Timing & loops' },
			{ slug: 'variations', label: 'Variations' },
			{ slug: 'lifetime', label: 'Lifetime & ownership' },
		],
	},
	{
		label: 'Beyond nodes',
		art: 'catalog',
		pages: [
			{ slug: 'materials', label: 'Materials' },
			{ slug: 'shaders', label: 'Shader uniforms' },
			{ slug: 'custom-tweens', label: 'Custom tweens', gdscript: 'Custom adapters' },
		],
	},
	{
		label: 'Reference',
		art: 'catalog',
		pages: [
			{
				label: 'Core API',
				pages: [
					{ slug: 'api', label: 'Overview' },
					{ slug: 'api/definitions', label: 'Definitions' },
					{ slug: 'api/handles', label: 'Handles & groups' },
					{ slug: 'api/scheduler', label: 'Scheduler' },
					{ slug: 'api/custom', label: 'Custom definitions', gdscript: 'Adapters' },
				],
			},
			{ slug: 'nodes', label: 'Node & value catalog', gdscript: 'Helper catalog' },
		],
	},
];

/** Pages that belong to neither language, listed under Project. The overview is reached from the site title. */
export const SHARED = [
	{ slug: 'faq', label: 'FAQ' },
	{ slug: 'compatibility', label: 'Compatibility' },
	{ slug: 'acknowledgements', label: 'Acknowledgements' },
];
export const SHARED_LABEL = 'Project';

/** 'csharp', 'gdscript', or undefined for a shared page, from a content id or URL path. */
export function trackOf(idOrPath) {
	const first = idOrPath.replace(/^\/+/, '').split('/')[0];
	return first in LANGS ? first : undefined;
}

/** The page slug within its track, e.g. 'definitions' for 'gdscript/definitions'. */
export const slugOf = (idOrPath) => idOrPath.replace(/^\/+|\/+$/g, '').split('/').slice(1).join('/');

/** Every page entry of a group, including those in nested groups. */
export const pagesOf = (group) => group.pages.flatMap((page) => (page.pages ? pagesOf(page) : [page]));

export const firstSlug = PATH[0].pages[0].slug;

const items = (pages, lang) =>
	pages.map((page) =>
		page.pages
			? { label: page[lang] ?? page.label, items: items(page.pages, lang) }
			: { label: page[lang] ?? page.label, slug: `${lang}/${page.slug}` },
	);

/** Starlight sidebar config: one top-level group per language, then the shared pages. */
export const sidebar = () => [
	...Object.entries(LANGS).map(([lang, { label, status }]) => ({
		label,
		...(status && { badge: { text: status, variant: 'note' } }),
		items: PATH.map((group) => ({ label: group.label, items: items(group.pages, lang) })),
	})),
	{ label: SHARED_LABEL, items: SHARED.map(({ slug, label }) => ({ label, slug })) },
];

/** Old single-path URLs, kept working after the split. */
export const redirects = {
	'/concepts/definitions': '/csharp/definitions/',
	'/concepts/easing': '/csharp/easing/',
	'/concepts/timing': '/csharp/timing/',
	'/concepts/lifetime': '/csharp/lifetime/',
	'/csharp': `/csharp/${firstSlug}/`,
	'/gdscript': `/gdscript/${firstSlug}/`,
};
