# Website utilities

Run Node and PowerShell commands from `docs/` after `npm ci`. Node 24 matches CI
and supports the renderers' direct TypeScript imports. Python runs through `uv`;
font tools require `fonttools[woff]`, which the SVG helpers request automatically.

| Utility                          | Entry point                                                                                                         | Purpose / output                                                                                                                                                                                                                 |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `check-built-doc-links.mjs`      | `npm run check:links`                                                                                               | After `npm run build`, inspect `dist/` HTML for missing local targets/anchors, private links, and starter content. Does not fetch external links.                                                                                |
| `check-easing-conformance.mjs`   | `npm run check:easing`                                                                                              | Verify the website's easing and effect math against shared conformance fixtures, endpoint rules, mirrors, and input validation.                                                                                                  |
| `check-search-upstream.mjs`      | `npm run check:search`; also imported by the Astro build hook                                                       | Compare installed Starlight search source to its reviewed checksum. Follow [search override maintenance](../README.md#search-override-maintenance) before updating the checksum.                                                 |
| `generate-llms.mjs`              | Imported by the Astro build hook                                                                                    | Convert rendered documentation to `dist/llms.txt`, `llms-full.txt`, and per-page `index.md` files. Preserve code and tables; follow the language tracks from `src/tracks.mjs`. Add Markdown discovery links to built HTML.       |
| `check-built-llms.mjs`           | `npm run check:llms`                                                                                                | After the build, verify sitemap coverage, export links, code fidelity, generated catalogs, and permissive robots directives. Also check HTML-to-Markdown conversion of code, tables, links, language labels, and heading levels. |
| `Test-CSharpDocExamples.ps1`     | `pwsh ./scripts/Test-CSharpDocExamples.ps1`                                                                         | Compile the C# fences from documentation against the library and Godot's source generators with .NET; writes scratch projects under `../artifacts/docs-examples/`. Does not execute snippets.                                    |
| `render-home-comparison-svg.mjs` | `npm run render:compare`                                                                                            | Extract the homepage's code blocks from `src/content/docs/index.mdx`, highlight them, and animate the three-sprite pair switching between C# and GDScript. Writes `public/compare.svg` and `.svgz`.                              |
| `render-easing-svg.mjs`          | `npm run render:easing`                                                                                             | Sample website easing math and reproduce `EasingPlayground.astro` as a looping image. Writes `public/easing.svg` and `.svgz`.                                                                                                    |
| `render-definitions-svg.mjs`     | `npm run render:definitions`                                                                                        | Reproduce `ReuseDemo.astro`: five icons sharing a definition with changing height, stagger, and language. Reads `public/godot.svg`; writes `public/definitions.svg` and `.svgz`.                                                 |
| `animated-svg-utils.mjs`         | Imported by the three renderers                                                                                     | Shared colors, syntax highlighting, XML escaping, font subsetting/measurement, timelines, keyframes, fading layers, sliders, switches, and SVG/gzip output. No standalone command.                                               |
| `measure-font-text.py`           | Called by `animated-svg-utils.mjs` through `uv`                                                                     | Accept a font path and JSON array of strings; print JSON advance widths in em without kerning. No file output.                                                                                                                   |
| `prepare-lexie-fonts.py`         | From the repository root: `uv run --no-project --with 'fonttools[woff]' python docs/scripts/prepare-lexie-fonts.py` | Add the OpenType `gasp` smoothing table to both bundled Lexie Readable fonts in place. Run when replacing those font files.                                                                                                      |

## Animated SVG maintenance

The three SVGs are self-contained images for the Godot Asset Store page. SVG images
cannot run JavaScript or load external webfonts, so these renderers embed subset
WOFF2 fonts and animate with CSS keyframes. Reduced motion displays a still frame.

Each renderer is organized as: example states and timing, layout geometry, code/font
preparation, animated elements and controls, then final SVG assembly. Coordinates
are in pixels; timeline values are in seconds. `animated-svg-utils.mjs` documents
the shared functions and their return values beside their definitions.

The comparison reads current homepage snippets. The easing and definitions renderers
mirror their widgets' layout and code; update them when their corresponding Astro
components change. They share the website's motion functions, code themes, and easing
constants where possible. Run all three `render:*` commands after changing shared
helpers, and review the emitted images with animation and reduced motion enabled.

All utilities are used by npm scripts, imports, CI, or documented font maintenance.
Formatting commands and platform requirements are in [scripts/README.md](../../scripts/README.md#formatting).
