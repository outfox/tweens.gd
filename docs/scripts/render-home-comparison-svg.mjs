// Renders the home page's comparison of Godot's Tween and tweens.gd as an animated SVG, public/compare.svg, for the
// Godot Asset Store page. It cycles C# and GDScript with one and three sprites. Needs uv on the PATH.
// Run from docs/: node scripts/render-home-comparison-svg.mjs
import { readFileSync } from 'node:fs';
import {
  EASE,
  FONTS,
  MARGIN,
  PILL,
  escapeXml,
  fontFace,
  highlight,
  languagePill,
  languages,
  measure,
  mix,
  palette,
  pill,
  pillStyle,
  round,
  reducedMotion,
  shadowFilter,
  subsetFont,
  timeline,
  writeSvg,
} from './animated-svg-utils.mjs';

const variants = ['One sprite', 'Three sprites'];
const order = [
  ['csharp', 0],
  ['csharp', 1],
  ['gdscript', 0],
  ['gdscript', 1],
];

// Seconds each example shows, and how long switches and code take to change.
const SLOT = 4.5;
const MOVE = 0.45;
const FADE_IN = 0.6;
const FADE_OUT = 0.35;
const STAGGER = 0.12;
const CYCLE = SLOT * order.length;
const tl = timeline(
  CYCLE,
  order.map((_, k) => k * SLOT),
  MOVE,
);
const pct = tl.pct;

// Layout in px, matching the site's code blocks: JetBrains Mono advances 0.6em.
const CODE = { size: 14, line: 22.4, advance: 14 * 0.6 };
const FRAME = { padX: 20, padY: 16, bar: 38, radius: 14.4, tabRadius: 8, title: 15, gap: 24 };

// The code blocks of index.mdx, in page order: one sprite, then three, each with a C# pair and a GDScript pair.
const mdx = readFileSync('src/content/docs/index.mdx', 'utf8');
const blocks = { csharp: [], gdscript: [] };
for (const [, lang, title, code] of mdx.matchAll(
  /```(csharp|gdscript) title="([^"]+)"\r?\n([\s\S]*?)\r?\n```/g,
))
  blocks[lang].push({ title, lines: code.replace(/\t/g, '    ').split(/\r?\n/) });
if (blocks.csharp.length !== 4 || blocks.gdscript.length !== 4)
  throw new Error('Expected 4 C# and 4 GDScript blocks.');

// Subsets each font to the characters it draws, and measures the display font's titles.
const titles = [
  ...new Set(
    Object.values(blocks)
      .flat()
      .map((b) => b.title),
  ),
];
const allCode = Object.values(blocks)
  .flat()
  .flatMap((b) => b.lines)
  .join('');
const pillText = [...Object.values(languages).map((l) => l.label), ...variants].join('');
const monoFont = subsetFont(FONTS.mono, allCode + pillText);
const displayFont = subsetFont(FONTS.display, titles.join(''));
const titleWidths = Object.fromEntries(
  measure(FONTS.display, titles).map((w, i) => [titles[i], w * FRAME.title]),
);

// Two equal columns, wide enough for the longest line of any example.
const longest = Math.max(
  ...Object.values(blocks)
    .flat()
    .flatMap((b) => b.lines.map((l) => l.length)),
);
const column = Math.ceil(longest * CODE.advance + 2 * FRAME.padX);
const width = 2 * MARGIN.x + 2 * column + FRAME.gap;
const frameTop = MARGIN.top + PILL.height + 18;
const frameHeight = (lines) => FRAME.bar + 2 * FRAME.padY + lines * CODE.line;
const tallest = Math.max(
  ...Object.values(blocks)
    .flat()
    .map((b) => frameHeight(b.lines.length)),
);
const height = Math.ceil(frameTop + tallest + MARGIN.bottom);

// One frame: a tab bar with the title's tab, then the highlighted code.
const frame = (x, block, tokens, lang) => {
  const { radius: R, tabRadius: t, bar } = FRAME;
  const y = frameTop;
  const w = column;
  const h = frameHeight(block.lines.length);
  const bg = mix(languages[lang].accent, 0.1, palette.stage);
  const tab = round(titleWidths[block.title] + 32);
  const lines = tokens.map((line, i) => {
    const baseline = round(y + bar + FRAME.padY + i * CODE.line + CODE.line / 2 + CODE.size * 0.36);
    const runs = line
      .map((tk) => `<tspan fill="${tk.color}">${escapeXml(tk.content)}</tspan>`)
      .join('');
    return `<text class="code" x="${x + FRAME.padX}" y="${baseline}">${runs}</text>`;
  });
  return `<g>
<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${R}" fill="${bg}" filter="url(#shadow)"/>
<path d="M${x} ${y + bar}V${y + R}a${R} ${R} 0 0 1 ${R}-${R}H${x + w - R}a${R} ${R} 0 0 1 ${R} ${R}V${y + bar}Z" fill="${palette.surface}"/>
<path d="M${x} ${y + bar}V${y + R}a${R} ${R} 0 0 1 ${R}-${R}H${x + tab - t}a${t} ${t} 0 0 1 ${t} ${t}V${y + bar}Z" fill="${bg}"/>
<rect x="${x + 0.5}" y="${y + 0.5}" width="${w - 1}" height="${h - 1}" rx="${R - 0.5}" fill="none" stroke="${palette.outline}"/>
<text class="title" x="${x + 16}" y="${round(y + bar / 2 + FRAME.title * 0.36)}">${escapeXml(block.title)}</text>
${lines.join('\n')}
</g>`;
};

const lang = languagePill(
  tl,
  MARGIN.x,
  MARGIN.top,
  order.map(([l]) => l),
);
const variantPill = pill(tl, {
  x: MARGIN.x + lang.width + 12,
  y: MARGIN.top,
  name: 'variant',
  labels: variants,
  picks: order.map(([, v]) => v),
  accents: order.map(([l]) => languages[l]),
});
const css = [...lang.css, ...variantPill.css];

// Each example fades and rises in, holds, then fades out upward while the switches move, before the next one arrives.
css.push(`@keyframes show {
	0% { opacity: 0; transform: translateY(14px); animation-timing-function: ${EASE.expoOut}; }
	${pct(FADE_IN)} { opacity: 1; transform: none; }
	${pct(SLOT - MOVE)} { opacity: 1; transform: none; animation-timing-function: ease-in; }
	${pct(SLOT - MOVE + FADE_OUT)} { opacity: 0; transform: translateY(-10px); }
	100% { opacity: 0; transform: translateY(-10px); }
}`);

const examples = [];
for (const [k, [lang, variant]] of order.entries()) {
  const [left, right] = blocks[lang].slice(variant * 2, variant * 2 + 2);
  const [leftTokens, rightTokens] = [
    await highlight(lang, left.lines),
    await highlight(lang, right.lines),
  ];
  for (const [i, block, tokens] of [
    [0, left, leftTokens],
    [1, right, rightTokens],
  ]) {
    const delay = round(k * SLOT + i * STAGGER);
    examples.push(`<g class="example" style="opacity: ${k === 0 ? 1 : 0}; animation-delay: ${delay}s">
${frame(MARGIN.x + i * (column + FRAME.gap), block, tokens, lang)}
</g>`);
  }
}

const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
<title>Godot's Tween next to tweens.gd, in C# and GDScript</title>
<style>
${fontFace('JetBrains Mono', '100 800', monoFont)}
${fontFace('Lexie Readable', 700, displayFont)}
text { white-space: pre; }
.code { font: 400 ${CODE.size}px 'JetBrains Mono', ui-monospace, monospace; }
.title { font: 700 ${FRAME.title}px 'Lexie Readable', system-ui, sans-serif; fill: ${palette.text}; }
${pillStyle}
.example { animation: show ${CYCLE}s infinite both; }
${css.join('\n')}
${reducedMotion}
</style>
<defs>
${shadowFilter}
</defs>
${lang.svg}
${variantPill.svg}
${examples.join('\n')}
</svg>
`;

writeSvg('public/compare.svg', svg, width, height);
