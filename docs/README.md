# tweens.gd public documentation

Astro/Starlight documentation for the beta C# library and beta GDScript addon.
Content lives in `src/content/docs/`. Internal working documents stay outside this site.

## Local development

From `docs/`, with a Node version supported by the locked Astro dependencies:

```powershell
npm ci
npm run dev -- --background
```

Manage the server with `npm run astro -- dev status`, `npm run astro -- dev logs`,
and `npm run astro -- dev stop`.

```powershell
npm run build
npm run check:links
npm run preview -- --background
```

The build emits `dist/`, including Pagefind search. The link check inspects
generated HTML targets and anchors; it also rejects internal-document links and
starter-template text. Preview the production build when checking search.

## Structure

The site has two parallel learning paths with the same page slugs: `csharp/<slug>` and
`gdscript/<slug>`. `src/tracks.mjs` lists the path once; `astro.config.mjs` builds the
sidebar from it, and `src/routeData.ts` shows each page only its own language's path, with
prev/next following that path. The overview, FAQ, compatibility, and acknowledgements
pages are shared; the sidebar lists all but the overview, which the site title links to.

- Add a page to both paths at the same slug and register it once in `src/tracks.mjs`.
  A language can relabel a page there (`gdscript: 'Groups & sequences'`).
- The language switch (`LangSwitch.astro`) links each page to its twin. On shared pages it
  sets the remembered language instead, and `<Lang only="csharp|gdscript">` blocks show
  the matching content.
- Switching opens the twin at the reader's section. Sections match by heading id, or by
  position when both twins have the same number of `##` (or `###`) headings, so keep a
  twin's sections in the same order even where their titles differ.
- Interactive demos are shared. Pass `lang="gdscript"` so their code and labels use
  GDScript names; `src/scripts/lang.ts` maps the C# names.
- Renamed URLs keep working through `redirects` in `src/tracks.mjs`.

## Content conventions

- Each path is a learning path: start here, write reusable tweens, shape the motion,
  beyond nodes, then reference. Prev/next links follow it.
- Each page opens with a one-paragraph lede stating its key idea; the theme sets it apart.
  Pitfalls go in `:::caution` asides so they stand out from the main flow.
- Annotated examples use Expressive Code line-marker labels (`{"1":3-7}`) inside `<Moves>`,
  whose numbered notes match the labels.
- Write each twin in its own language's idioms. Where the languages behave differently,
  say so on that page rather than sharing prose that fits neither.
- C# snippets state their prerequisites; GDScript pages state the `preload` they assume.
- GDScript API facts come from `addons/tweens_gd/README.md` and the addon source. The
  helper catalog is generated at build time from `addons/tweens_gd/CATALOG.md`.
- Keep release procedures, coverage reports, and implementation notes private.
- Keep library versions, adapter names, and examples aligned with the source.
- Check C# code fences with `pwsh ./scripts/Check-Examples.ps1` from this directory.
  It compiles snippets against the library using explicit context for fragments;
  it does not execute native examples or certify their rendered output. GDScript
  fences are not checked automatically yet.

The public URL is `https://tweens.gd`, set as Astro `site` for canonical URLs and
the sitemap. Links assume that domain-root deployment. statichost.eu builds and
deploys the site automatically from a repository webhook; no workflow is needed here.

`public/_headers` sets statichost's response headers. Files whose names carry a content
hash (Astro's `/_astro/` output, Pagefind's fragments and index chunks) are cached as
immutable, so a page navigation doesn't revalidate its stylesheets, scripts, and fonts.
Pages and unhashed files keep statichost's default, revalidated on every request.
