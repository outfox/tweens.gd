import { defineRouteMiddleware, type StarlightRouteData } from '@astrojs/starlight/route-data';
import { LANGS, LEARN, hubOf, slugOf, trackOf } from './tracks.mjs';
import { catalogSection } from './scripts/gd-catalog';

type Entry = StarlightRouteData['sidebar'][number];
type Link = Extract<Entry, { type: 'link' }>;

const links = (entries: Entry[]): Link[] => entries.flatMap((e) => (e.type === 'link' ? [e] : links(e.entries)));

/** Marks every link of a track with its language, so theme.css can hide the other track on a hub page. Starlight marks
 *  only the first link to the hub as current; `here` marks its copy in each track. */
const tag = (entries: Entry[], lang: string, here: string): Entry[] =>
	entries.map((e) =>
		e.type === 'link'
			? { ...e, isCurrent: e.isCurrent || e.href === here, attrs: { ...e.attrs, 'data-track': lang } }
			: { ...e, entries: tag(e.entries, lang, here) },
	);

// The configured sidebar holds one group per language, then the shared pages (src/tracks.mjs).
// A language page sees only its own track; a hub page sees both, and the reader's language picks one.
// Prev/next follows the five Learn pages; guides and reference are optional destinations, not further tutorial steps.
export const onRequest = defineRouteMiddleware((context) => {
	const route = context.locals.starlightRoute;
	const languages = Object.keys(LANGS).length;
	const tracks = route.sidebar.slice(0, languages);
	const shared = route.sidebar.slice(languages);
	const lang = trackOf(route.id);
	const index = Object.keys(LANGS).indexOf(lang ?? '');
	const track = tracks[index];

	if (track?.type === 'group') {
		route.sidebar = [...track.entries, ...shared.map((g) => ({ ...g, collapsed: true }))];
		const path = links(track.entries).filter((link) =>
			LEARN.some((page) => page.slug === slugOf(link.href)),
		);
		const i = path.findIndex((l) => l.isCurrent);
		route.pagination = { prev: path[i - 1], next: i >= 0 ? path[i + 1] : undefined };
	} else if (hubOf(route.id)) {
		const langs = Object.keys(LANGS);
		const here = hubOf(route.id)!.link;
		const entries = tracks.flatMap((t, i) => (t.type === 'group' ? tag(t.entries, langs[i], here) : []));
		route.sidebar = [...entries, ...shared.map((g) => ({ ...g, collapsed: true }))];
		route.pagination = { prev: undefined, next: undefined };
	} else {
		route.sidebar = [...tracks.map((g) => ({ ...g, collapsed: true })), ...shared];
		route.pagination = { prev: undefined, next: undefined };
	}

	// A GDScript catalog page renders its class headings in GdCatalog.astro, out of the table of contents' sight; list
	// them after Overview, where the component sits ahead of the page's own headings.
	const section = route.entry.data.catalog;
	if (section && route.toc) {
		const { targets, single } = catalogSection(section);
		const top = route.toc.items.findIndex((item) => item.slug === '_top') + 1;
		if (!single)
			route.toc.items.splice(top, 0, ...targets.map((t) => ({ depth: 2, slug: t.anchor, text: t.heading, children: [] })));
	}
});
