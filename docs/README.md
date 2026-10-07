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

See [the website utility catalog](scripts/README.md) for each script's purpose,
inputs, outputs, and SVG maintenance notes, and [script formatting](../scripts/README.md#formatting)
for the repeatable Prettier, Ruff, and PowerShell commands.

`npm run render:compare` redraws `public/compare.svg`, the home page comparison as an animated image for the Godot
Asset Store page. Rerun it after changing the comparison's code in `index.mdx`; it needs uv for font subsetting.
`npm run render:easing` and `npm run render:definitions` do the same for the easing playground and the definitions
demo, as `public/easing.svg` and `public/definitions.svg`. Their scripts copy the layout and code of
`EasingPlayground.astro` and `ReuseDemo.astro`, so update them along with those widgets. Each script also writes a
gzipped `.svgz` copy beside its SVG.

## Structure

The site has two parallel learning paths with the same page slugs: `csharp/<slug>` and
`gdscript/<slug>`. `src/tracks.mjs` lists the path once; `astro.config.mjs` builds the
sidebar from it, and `src/routeData.ts` shows each page only its own language's track.
Only the five `LEARN` pages have prev/next links; the homepage uses that same list.
Guides, Reference, and Project start collapsed, with the current page's group opened.
The overview, FAQ, compatibility, advanced development, and acknowledgements
pages are shared; the sidebar lists all but the overview, which the site title links to.

- Add a page to both paths at the same slug and register it once in `src/tracks.mjs`.
  A language can relabel a page there (`gdscript: 'Sequences'`).
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
- The tutorial runs Installation → Your First Tween → Reuse a Definition → Syntax & Sugar → Await Completion. The sidebar
  numbers these steps; their labels use title case.
  Sequences is an optional guide. Tweening opens with Anatomy/Definitions (`anatomy`), one schematic card per definition field
  (`scripts/anatomy.ts`), then Easings (the playground), Effects, Functions, and Curves; Porting, a shared page,
  maps Godot's Tween eases. A new definition field needs a card there.
- Guides use Starlight `Aside` for actionable hints and pitfalls, and `Card`/`LinkCard`
  for choices and next steps. Keep the main flow to one example per concept; components
  should make the page easier to scan, not hide a long explanation.
- Materials and shader uniforms share one guide. The old shader URLs redirect there;
  exhaustive binding and restoration rules live in the material reference.
- Keep setup exceptions and library development notes under Project → Advanced development.
- Teach one concept with one example before adding variations. Move exhaustive rules
  into reference, and link to them from the guide. Update links when moving content; the site is
  too new to need placeholder anchors for old section links.
- Keep language twins in the same section order. Give demos one concrete experiment
  instead of repeating an explanation of every control.
- Enable `tableOfContents: true` on reference pages with several sections.
- Core API pages follow a tween's life: describe, shape, start, control, extend. Each opens with a lede that links
  the guide teaching it, then at most one short snippet, member tables, and a closing list of rules. A fact lives on
  one page, and each enum on the page it belongs to (`FillMode` on Timing, `Reason` on Handles). Tables whose first
  column is one of `MEMBER_COLUMNS` (`astro.config.mjs`) render as member lists with a link per row, so keep member
  names unique on a page.
- `<Timeline>` draws Chain entries or a tween's cycles on one time axis; `<EaseFamilies>` draws every easing family
  from `src/scripts/motion.ts`.
- Each page opens with a one-paragraph lede stating its key idea; the theme sets it apart.
  Lead pages and paragraphs with the reader's goal or the visible result. Introduce
  technical details after their purpose, and place prerequisites beside the step
  that needs them. Keep edge cases out of introductions.
  Pitfalls go in `:::caution` asides so they stand out from the main flow.
- Annotated examples use Expressive Code line-marker labels (`{"1":3-7}`) inside `<Moves>`,
  whose numbered notes match the labels.
- Separate a definition from its start with a blank line, as in the define, start, await examples.
- Two ways to write the same thing go in `<Compare>`. A `---` line splits it into two sides; markdown before or
  after a side's one code block sits above or below it, lined up with the other side.
- Comparisons with Godot's `Tween` go in `<Compare>`, Godot first. Where reuse is the point, start one
  definition on `sprite1` and `sprite2`. `<Variants>` switches between versions of one example.
- Write each twin in its own language's idioms. Where the languages behave differently,
  say so on that page rather than sharing prose that fits neither.
- C# snippets state their prerequisites; GDScript pages state the `preload` they assume.
- GDScript API facts come from the addon source and the class reference in `gdextension/doc_classes/`. The
  helper catalog is generated at build time from `addons/tweens_gd/CATALOG.md`: a page names its
  section in the `catalog` frontmatter field, `<GdCatalog />` renders it, and `src/routeData.ts` adds its
  class headings to the table of contents.
- Keep release procedures, coverage reports, and implementation notes private.
- Keep library versions, adapter names, and examples aligned with the source.
- Check C# code fences with `pwsh ./scripts/Test-CSharpDocExamples.ps1` from this directory.
  It compiles snippets against the library using explicit context for fragments;
  it does not execute native examples or certify their rendered output. GDScript
  fences are not checked automatically yet.

The public URL is `https://tweens.gd`, set as Astro `site` for canonical URLs and
the sitemap. Links assume that domain-root deployment. statichost.eu builds and
deploys the site automatically from a repository webhook. CI's docs job runs the build, checks, and C# example
compile when the site or the library sources it reads change.

`public/_headers` sets statichost's response headers. Files whose names carry a content
hash (Astro's `/_astro/` output, Pagefind's fragments and index chunks) are cached as
immutable, so a page navigation doesn't revalidate its stylesheets, scripts, and fonts.
Pages and unhashed files stay fresh for 60 seconds, then revalidate. This short window
lets Firefox reuse a hovered link's prefetched HTML instead of making another network
request on click; published changes can take up to a minute to appear in an existing cache.
`Head.astro` keeps Speculation Rules prefetch for browsers that support it and opts
eligible links into Astro's hover/focus prefetch in other browsers. Shared-page language
toggles, downloads, external links, and current-page anchors are excluded from the fallback.
