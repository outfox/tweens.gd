// Two language tracks share page slugs. Only Learn is sequential; Easing, Guides and Reference are optional.
// astro.config.mjs builds the sidebar from this list; the route middleware, the language switch, and the section art
// read it too, so a page and its twin always sit at the same place in each path.

export const LANGS = {
	csharp: { label: 'C#', long: 'C#', status: 'Beta' },
	gdscript: { label: 'GDScript', long: 'GDScript', status: 'Beta' },
};

/** Groups in path order. `art` picks the drawing beside each page title; `gdscript` overrides a label there.
 *  An entry with `pages` instead of a `slug` is a nested sidebar group; one with a `link` is a hub page (see HUBS). */
export const PATH = [
	{
		label: 'Learn',
		art: 'hop',
		pages: [
			{ link: '/tutorial/', label: 'Tutorial' },
			{ slug: 'installation', label: 'Install' },
			{ slug: 'quickstart', label: 'Your first tween' },
			{ slug: 'definitions', label: 'Definitions' },
			{ slug: 'sequences', label: 'Sequences', gdscript: 'Sequences' },
			{ slug: 'playback', label: 'Control & completion' },
		],
	},
	{
		label: 'Easing',
		art: 'curve',
		pages: [
			{ link: '/easings/', label: 'Playground' },
			{ slug: 'custom', label: 'Custom curves' },
		],
	},
	{
		label: 'Guides',
		art: 'sketch',
		collapsed: true,
		pages: [
			{ slug: 'syntax-sugar', label: 'Syntax sugar' },
			{ slug: 'timing', label: 'Timing & loops' },
			{ slug: 'variations', label: 'Variations' },
			{ slug: 'lifetime', label: 'Lifetime & ownership' },
			{ slug: 'cancellation', label: 'Cancellation & reasons' },
			{ slug: 'materials', label: 'Materials' },
			{ slug: 'shaders', label: 'Shader uniforms' },
			{ slug: 'custom-tweens', label: 'Custom tweens', gdscript: 'Custom adapters' },
		],
	},
	{
		label: 'Reference',
		art: 'catalog',
		collapsed: true,
		pages: [
			{ label: 'Core API', pages: [
				{ slug: 'api', label: 'Overview' },
				{ slug: 'api/definitions', label: 'Creating definitions' },
				{ slug: 'api/endpoints', label: 'Endpoints & variations' },
				{ slug: 'api/timing', label: 'Timing & easing' },
				{ slug: 'api/easing', label: 'Easing reference' },
				{ slug: 'api/effects', label: 'Effect factories' },
				{ slug: 'api/modes', label: 'Modes & callbacks' },
				{ slug: 'api/enums', label: 'Enums', gdscript: 'Constants' },
				{ slug: 'api/handles', label: 'Handles' },
				{ slug: 'api/groups', label: 'Groups' },
				{ slug: 'api/chains', label: 'Chains' },
				{ slug: 'api/scheduler', label: 'Scheduler' },
				{ slug: 'api/custom', label: 'Custom definitions', gdscript: 'Adapters' },
			] },
			{ label: 'Property catalog', pages: [
				{ slug: 'nodes', label: 'Overview' },
				{ slug: 'nodes/2d', label: '2D nodes' },
				{ slug: 'nodes/3d', label: '3D nodes' },
				{ slug: 'nodes/ui', label: 'UI controls' },
				{ slug: 'nodes/materials', label: 'Material properties' },
				{ slug: 'nodes/audio', label: 'Animation & audio' },
				{ slug: 'nodes/values', label: 'Callback values' },
			] },
		],
	},
];

/** Only these pages form the sequential tutorial, also shown on the tutorial hub. */
export const LEARN = PATH[0].pages.filter((page) => page.slug);

/** Pages that belong to neither language, listed under Project. The overview is reached from the site title. */
export const SHARED = [
	{ slug: 'faq', label: 'FAQ' },
	{ slug: 'compatibility', label: 'Compatibility' },
	{ slug: 'advanced-development', label: 'Advanced development' },
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

export const firstSlug = LEARN[0].slug;

/** Hub pages are shared pages that both tracks list, such as the tutorial overview and the easing playground. */
export const HUBS = PATH.flatMap(pagesOf).filter((page) => page.link);

/** The hub entry for a content id such as 'tutorial', if the page is one. */
export const hubOf = (id) => HUBS.find((page) => page.link === `/${id}/`);

const items = (pages, lang) =>
	pages.map((page) =>
		page.pages
			? { label: page[lang] ?? page.label, collapsed: true, items: items(page.pages, lang) }
			: page.link
				? { label: page[lang] ?? page.label, link: page.link }
				: { label: page[lang] ?? page.label, slug: `${lang}/${page.slug}` },
	);

/** Starlight sidebar config: one top-level group per language, then the shared pages. */
export const sidebar = () => [
	...Object.entries(LANGS).map(([lang, { label, status }]) => ({
		label,
		...(status && { badge: { text: status, variant: 'note' } }),
		items: PATH.map((group) => ({ label: group.label, collapsed: group.collapsed ?? false, items: items(group.pages, lang) })),
	})),
	{ label: SHARED_LABEL, collapsed: true, items: SHARED.map(({ slug, label }) => ({ label, slug })) },
];

/** Old single-path URLs, kept working after the split. */
export const redirects = {
	'/concepts/definitions': '/csharp/definitions/',
	'/concepts/easing': '/easings/',
	'/concepts/timing': '/csharp/timing/',
	'/concepts/lifetime': '/csharp/lifetime/',
	'/csharp': `/csharp/${firstSlug}/`,
	'/gdscript': `/gdscript/${firstSlug}/`,
};
