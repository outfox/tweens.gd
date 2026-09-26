import { defineRouteMiddleware, type StarlightRouteData } from '@astrojs/starlight/route-data';
import { LANGS, trackOf } from './tracks.mjs';

type Entry = StarlightRouteData['sidebar'][number];
type Link = Extract<Entry, { type: 'link' }>;

const links = (entries: Entry[]): Link[] => entries.flatMap((e) => (e.type === 'link' ? [e] : links(e.entries)));

// The configured sidebar holds one group per language, then the shared pages (src/tracks.mjs).
// A language page sees only its own path, with prev/next following that path. A shared page lists both paths
// collapsed; its pagination is dropped, since neither path is "next" from there.
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
		const path = links(track.entries);
		const i = path.findIndex((l) => l.isCurrent);
		route.pagination = { prev: path[i - 1], next: i >= 0 ? path[i + 1] : undefined };
	} else {
		route.sidebar = [...tracks.map((g) => ({ ...g, collapsed: true })), ...shared];
		route.pagination = { prev: undefined, next: undefined };
	}
});
