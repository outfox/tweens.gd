// Renders the definitions demo (ReuseDemo.astro) as an animated SVG, public/definitions.svg, for the Godot Asset Store
// page. Five icons hop in waves from one definition while its height and each copy's stagger change, in C# and
// GDScript. Needs uv on the PATH. Run from docs/: node scripts/render-definitions-svg.mjs
import { readFileSync } from 'node:fs';
import {
  EASE,
  FONTS,
  MARGIN,
  PILL,
  fades,
  fontFace,
  frames,
  collectGlyphs,
  highlight,
  languagePill,
  languages,
  mix,
  palette,
  pillStyle,
  round,
  range,
  reducedMotion,
  shadowFilter,
  steps,
  subsetFont,
  timeline,
  writeSvg,
} from './animated-svg-utils.mjs';

// Edits in order, one during each wave; a wave plays the settings it started with. Value edits land while icons are in
// the air, and a language switch at the wave's end. The edits must return to the start.
const START = { lang: 'csharp', height: 80, stagger: 0.2 };
const EDITS = [
  { height: 140 },
  { stagger: 0.08 },
  { lang: 'gdscript' },
  { height: 80 },
  { stagger: 0.2 },
  { lang: 'csharp' },
];

// Seconds, as in the demo: a 0.25-second Out.Quad hop there and back, then a rest before the next wave.
const DURATION = 0.25;
const REST = 0.8;
const ICONS = 5;
const EDIT_AT = 0.75;
const MOVE = 0.45;
// The frame shown under reduced motion, mid-wave as in the demo.
const STILL = 0.3;
// Out.Quad and its reverse, exactly.
const QUAD_OUT = 'cubic-bezier(0.333, 0.667, 0.667, 1)';
const QUAD_IN = 'cubic-bezier(0.333, 0, 0.667, 0.333)';

const waves = [];
const states = [{ ...START, at: 0 }];
let settings = START,
  CYCLE = 0;
for (const [j, edit] of EDITS.entries()) {
  const length = (ICONS - 1) * settings.stagger + 2 * DURATION + REST;
  waves.push({ ...settings, at: CYCLE });
  settings = { ...settings, ...edit };
  const at = edit.lang ? CYCLE + length : CYCLE + EDIT_AT;
  CYCLE += length;
  // The last edit is the switch back into the first state, as the cycle ends.
  if (j < EDITS.length - 1) states.push({ ...settings, at, edit: Object.keys(edit)[0] });
}
if (JSON.stringify(settings) !== JSON.stringify(START) || EDITS.at(-1).lang === undefined)
  throw new Error('The edits must end with a language switch back to the start.');
const tl = timeline(
  CYCLE,
  states.map((s) => s.at),
  MOVE,
);

// Layout in px, from the live widget at 1037 px wide; positions inside it are from its top-left corner.
const WIDGET = { x: MARGIN.x, y: MARGIN.top + PILL.height + 18, w: 1037, h: 390.9, radius: 17.6 };
const CODE = { x: 21, y: 21, w: 421.4, h: 250.3, textX: 37, top: 33.8, size: 12.48, line: 22.464 };
CODE.advance = CODE.size * 0.6;
const STAGE = { x: 482.4, y: 46.1, w: 533.6, h: 200.1 };
// The demo's scene is 960×360 of its own pixels: 128-pixel icons on a floor, 176 pixels apart.
const SCALE = STAGE.w / 960;
const iconX = (i) => 128 + i * 176;
const ROLES = { height: palette.amber, stagger: palette.blue };

// The demo's code, with the height and stagger it shows.
const code = ({ lang, height, stagger }) => {
  const gd = lang === 'gdscript';
  const values = { height: gd ? (-height).toFixed(1) : String(-height), stagger: String(stagger) };
  const lines = gd
    ? [
        'var hop := Tweens.position_2d_y()',
        'hop.by_value = {height}',
        'hop.duration = 0.25',
        'hop.ease = Out.QUAD',
        'hop.ping_pong = true',
        '',
        'for i in icons.size():',
        '    Tweens.play(icons[i], hop.with_delay(i * {stagger}))',
      ]
    : [
        'var hop = new Tweens.Position2DY',
        '{',
        '    By = {height},',
        '    Duration = 0.25,',
        '    Ease = Out.Quad,',
        '    PingPong = true,',
        '};',
        '',
        'for (var i = 0; i < icons.Length; i++)',
        '    icons[i].Tween(hop with { Delay = i * {stagger} });',
      ];
  const roles = {};
  lines.forEach((line, i) => {
    for (const role of Object.keys(values))
      if (line.includes(`{${role}}`))
        roles[role] = { line: i, column: line.indexOf(`{${role}}`), text: values[role] };
  });
  return {
    lines: lines.map((line) => line.replace(/\{(\w+)\}/g, (_, role) => values[role])),
    roles,
  };
};
const codes = states.map(code);

const { use, of } = collectGlyphs();
const css = [];
const swap = (name, perState) => {
  const swapped = fades(tl, name, perState);
  css.push(...swapped.css);
  return swapped.svg;
};

// Code lines, each value underlined in its slider's color.
const lineTop = (i) => CODE.top + i * CODE.line;
const baseline = (i) => round(lineTop(i) + CODE.line / 2 + CODE.size * 0.36);
const codeLines = await Promise.all(
  states.map(async (state, k) => {
    const { lines, roles } = codes[k];
    return (await highlight(state.lang, lines)).map((tokens, i) => {
      const runs = tokens
        .map((tk) => `<tspan fill="${tk.color}">${use('mono', tk.content)}</tspan>`)
        .join('');
      const underlines = Object.entries(roles)
        .filter(([, at]) => at.line === i)
        .map(
          ([role, at]) =>
            `<rect x="${round(CODE.textX + at.column * CODE.advance)}" y="${round(baseline(i) + 5)}" width="${round(at.text.length * CODE.advance)}" height="2" fill="${ROLES[role]}"/>`,
        );
      return `${underlines.join('')}<text class="code" x="${CODE.textX}" y="${baseline(i)}">${runs}</text>`;
    });
  }),
);
// An edited value flashes in its color, as the demo does.
const flashes = states.map((state, k) => {
  if (!ROLES[state.edit]) return '';
  const at = codes[k].roles[state.edit];
  const name = `flash-${k}`;
  const points = [
    [state.at - 0.01, 'opacity: 0'],
    [state.at, 'opacity: 1', EASE.expoOut],
    [state.at + 1, 'opacity: 0'],
  ];
  css.push(frames(tl, name, points), `.${name} { animation: ${name} ${CYCLE}s infinite; }`);
  return `<rect class="${name}" x="${round(CODE.textX + at.column * CODE.advance)}" y="${round(lineTop(at.line) + 2)}" width="${round(at.text.length * CODE.advance)}" height="17" rx="4" fill="${ROLES[state.edit]}" fill-opacity="0.35" style="opacity: 0"/>`;
});
const codeBg = (state) =>
  mix(mix(languages[state.lang].accent, 0.1, palette.bg), 0.75, palette.stage);
css.push(
  steps(tl, 'code-bg', 'fill', states.map(codeBg), 'linear'),
  `.code-bg { animation: code-bg ${CYCLE}s infinite; }`,
);

// Each icon hops in every wave, and its shadow on the floor shrinks and fades as it rises.
const lift = (wave, i, t) => {
  const local = t - wave.at - i * wave.stagger;
  const p =
    local <= 0 || local >= 2 * DURATION
      ? 0
      : local < DURATION
        ? local / DURATION
        : 2 - local / DURATION;
  return wave.height * (1 - (1 - p) ** 2);
};
const shadowAt = (h) => `transform: scale(${round(1 - h / 320)}); opacity: ${round(1 - h / 200)}`;
const hops = (i, name, at) => {
  const points = waves.flatMap((wave) => {
    const t = wave.at + i * wave.stagger;
    return [
      [t, at(0), QUAD_OUT],
      [t + DURATION, at(wave.height), QUAD_IN],
      [t + 2 * DURATION, at(0)],
    ];
  });
  css.push(frames(tl, name, points), `.${name} { animation: ${name} ${CYCLE}s infinite; }`);
};
const icons = Array.from({ length: ICONS }, (_, i) => {
  hops(i, `hop-${i}`, (h) => `transform: translateY(${-h}px)`);
  hops(i, `shadow-${i}`, shadowAt);
  const still = lift(waves[0], i, STILL);
  return `<ellipse class="shadow-${i}" cx="${iconX(i)}" cy="312" rx="52" ry="8" fill="${palette.outline}" fill-opacity="0.8" style="${shadowAt(still)}"/>
<g class="hop-${i}" style="transform: translateY(${round(-still)}px)"><use href="#godot" x="${iconX(i) - 64}" y="184" width="128" height="128" filter="url(#icon-shadow)"/></g>`;
});
const delays = swap(
  'delay',
  states.map((s) =>
    Array.from(
      { length: ICONS },
      (_, i) =>
        `<text class="delay" x="${iconX(i)}" y="${round(338 + (12 / SCALE) * 0.36)}">${use('mono', `${(i * s.stagger).toFixed(2)} s`)}</text>`,
    ),
  ),
);
const grid = [
  ...Array.from({ length: 15 }, (_, i) => `M${round(i * 64 + 0.9 / SCALE)} 0V360`),
  ...Array.from({ length: 6 }, (_, i) => `M0 ${round(i * 64 + 0.9 / SCALE)}H960`),
].join('');

// The sliders, labelled with where each edit goes.
const control = (role, x, w, title, where, fractions, outputs) => {
  const input = range(tl, {
    name: role,
    x,
    w,
    cy: 323.8,
    fractions,
    fills: states.map(() => ROLES[role]),
  });
  css.push(...input.css);
  return `<circle cx="${x + 4.8}" cy="299.9" r="4.8" fill="${ROLES[role]}"/>
<text class="label" x="${x + 16}" y="304.6">${use('text', title)}<tspan class="where" dx="6.4">${use('text', where)}</tspan></text>
${swap(
  `${role}-out`,
  outputs.map((v) => [`<text class="output" x="${x + w}" y="304.6">${use('mono', v)}</text>`]),
)}
${input.svg}`;
};
const controls = `${control(
  'height',
  21,
  485.5,
  'Height',
  'on the definition',
  states.map((s) => (s.height - 40) / 120),
  states.map((s) => `${s.height} px`),
)}
${control(
  'stagger',
  530.5,
  485.5,
  'Stagger',
  'on each copy',
  states.map((s) => s.stagger / 0.5),
  states.map((s) => `${s.stagger.toFixed(2)} s`),
)}
<text class="label" x="21" y="363.5">${use('text', 'Changes apply to the next wave. Icons in the air keep the settings they started with.')}</text>`;

const lang = languagePill(
  tl,
  MARGIN.x,
  MARGIN.top,
  states.map((s) => s.lang),
);
css.unshift(...lang.css);
const godot = readFileSync('public/godot.svg', 'utf8').replace(/^<svg[^>]*>|<\/svg>\s*$/g, '');
const width = WIDGET.x + WIDGET.w + MARGIN.x;
const height = Math.ceil(WIDGET.y + WIDGET.h + MARGIN.bottom);

const widget = `<g transform="translate(${WIDGET.x} ${WIDGET.y})">
<rect x="0.5" y="0.5" width="${WIDGET.w - 1}" height="${WIDGET.h - 1}" rx="${WIDGET.radius}" fill="${palette.stage}" stroke="${palette.outline}" filter="url(#shadow)"/>
<rect class="code-bg" x="${CODE.x}" y="${CODE.y}" width="${CODE.w}" height="${CODE.h}" rx="12.8" fill="${codeBg(states[0])}"/>
${flashes.filter(Boolean).join('\n')}
${swap('code', codeLines)}
<svg x="${STAGE.x}" y="${STAGE.y}" width="${STAGE.w}" height="${STAGE.h}" viewBox="0 0 960 360">
<clipPath id="stage-clip"><rect width="960" height="360" rx="${round(12.8 / SCALE)}"/></clipPath>
<g clip-path="url(#stage-clip)">
<rect width="960" height="360" fill="${palette.bg}"/>
<path d="${grid}" stroke="${palette.outline}" stroke-opacity="0.55" stroke-width="${round(1 / SCALE)}"/>
<rect y="312" width="960" height="${round(2 / SCALE)}" fill="${palette.outline}"/>
${icons.join('\n')}
${delays}
</g>
</svg>
${controls}
</g>`;

const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
<title>One tweens.gd definition started on five icons, with its code in C# and GDScript</title>
<style>
${fontFace('JetBrains Mono', '100 800', subsetFont(FONTS.mono, of('mono') + lang.text))}
${fontFace('Lexie Readable', 400, subsetFont(FONTS.text, of('text')))}
text { white-space: pre; }
.code { font: 400 ${CODE.size}px 'JetBrains Mono', ui-monospace, monospace; }
.delay { font: 400 ${round(12 / SCALE)}px 'JetBrains Mono', ui-monospace, monospace; fill: ${palette.blueInk}; text-anchor: middle; }
.output { font: 400 13px 'JetBrains Mono', ui-monospace, monospace; fill: ${palette.white}; text-anchor: end; }
.label { font: 400 13px 'Lexie Readable', system-ui, sans-serif; fill: ${palette.muted}; }
.where { fill-opacity: 0.8; }
${icons.map((_, i) => `.shadow-${i}`).join(', ')} { transform-box: fill-box; transform-origin: center; }
${pillStyle}
${css.join('\n')}
${reducedMotion}
</style>
<defs>
${shadowFilter}
<filter id="icon-shadow" x="-20%" y="-20%" width="140%" height="150%">
<feDropShadow dx="0" dy="${round(3 / SCALE)}" stdDeviation="${round(3 / SCALE)}" flood-color="#03080e" flood-opacity="0.35"/>
</filter>
<symbol id="godot" viewBox="0 0 128 128">${godot}</symbol>
</defs>
${lang.svg}
${widget}
</svg>
`;

writeSvg('public/definitions.svg', svg, width, height);
