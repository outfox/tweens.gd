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
npm run check:easing
npm run preview -- --background
```

The build emits `dist/`, including Pagefind search. The link check inspects
generated HTML targets and anchors; it also rejects internal-document links and
starter-template text. Preview the production build when checking search.

## Structure

The site has two parallel learning paths with the same page slugs: `csharp/<slug>` and
`gdscript/<slug>`. `src/tracks.mjs` lists the path once; `astro.config.mjs` builds the
sidebar from it, and `src/routeData.ts` shows each page only its own language's track.
Only the five `LEARN` pages have prev/next links; the homepage uses that same list.
Guides, Reference, and Project start collapsed, with the current page's group opened.
The overview, FAQ, compatibility, advanced development, and acknowledgements
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
- Search includes the selected language's path and shared pages. `Head.astro` tags pages
  for Pagefind; `overrides/Search.astro` applies the filter and follows `tweens:lang` changes.
  The search override retains Starlight 0.42.4's UI; every Astro build checks for upstream drift.

## Search override maintenance

Starlight's search component does not expose its Pagefind UI instance, so the language
filter currently needs a local component override. `scripts/check-search-upstream.mjs`
checks the installed upstream component against the reviewed source's SHA-256, ignoring
only CRLF/LF differences. An Astro build hook fails the build if that source changes,
including when running `astro build` directly. Run `npm run check:search` to check it alone.

When this check fails after an upgrade:

1. Compare the installed `@astrojs/starlight/components/Search.astro` with
   `src/components/overrides/Search.astro`. The checker prints the upstream file location.
2. Port upstream fixes while retaining the language filter, or remove the override and
   this check if Starlight now provides a suitable extension hook.
3. Verify search in both languages, shared-page switching with an existing query,
   remembered language selection, and pagination in a production preview.
4. Update `reviewedHash` to the checksum printed by the checker after reviewing the
   changes, update the version in the override's attribution, and rebuild.

## Content conventions

- Learn is a five-step tutorial with a definite endpoint. Guides answer optional questions;
  Reference contains complete member lists and detailed rules.
- Keep setup exceptions and library development notes under Project → Advanced development.
- Teach one concept with one example before adding variations. Move exhaustive rules
  into reference, and link to them from the guide. Preserve old section anchors when moving content.
- Keep language twins in the same section order. Give demos one concrete experiment
  instead of repeating an explanation of every control.
- Enable `tableOfContents: true` on reference pages with several sections.
- Each page opens with a one-paragraph lede stating its key idea; the theme sets it apart.
  Lead pages and paragraphs with the reader's goal or the visible result. Introduce
  technical details after their purpose, and place prerequisites beside the step
  that needs them. Keep edge cases out of introductions.
  Pitfalls go in `:::caution` asides so they stand out from the main flow.
- Annotated examples use Expressive Code line-marker labels (`{"1":3-7}`) inside `<Moves>`,
  whose numbered notes match the labels.
- Separate a definition from its start with a blank line, as in the define, start, await examples.
- Comparisons with Godot's `Tween` go in `<Compare>`, Godot first. Where reuse is the point, start one
  definition on `sprite1` and `sprite2`. `<Variants>` switches between versions of one example.
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
