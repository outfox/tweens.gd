// The GDScript helper catalog, read from the addon's generated CATALOG.md at build time and grouped into the same pages
// as the C# catalog. GdCatalog.astro renders a page's tables; the route middleware lists its class headings in the
// table of contents, which can't see headings that a component renders.
import catalog from '../../../addons/tweens_gd/CATALOG.md?raw';

export interface Row {
	helper: string;
	/** The paths the helper writes; empty for callback-value helpers. */
	property: string[];
	target: string;
	value: string;
}
export interface Target {
	name: string;
	heading: string;
	anchor: string;
	rows: Row[];
}
export interface CatalogSection {
	targets: Target[];
	count: number;
	/** A page for one target class needs no index or class headings. */
	single: boolean;
	/** Callback-value helpers write no property, so their page drops the column. */
	withProperty: boolean;
}

const cell = (text: string) => text.trim().replace(/^`|`$/g, '');
const rows: Row[] = catalog
	.replace(/\r\n/g, '\n')
	.split('\n')
	.filter((line) => line.startsWith('|') && !/^\|\s*(Helper|-)/.test(line))
	.map((line) => {
		const cells = line.split('|').slice(1, 5);
		const [helper, , target, value] = cells.map(cell);
		const property = [...cells[1].matchAll(/`([^`]+)`/g)].map((match) => match[1]);
		return { helper, property, target, value };
	})
	.filter((row) => row.helper && row.target);

// Page sections in the order of the C# catalog; classes missing here land under "Other".
const SECTIONS: [string, string[]][] = [
	[
		'2D',
		[
			'CanvasItem', 'Node2D', 'AnimatedSprite2D', 'Camera2D', 'CanvasLayer', 'CanvasModulate', 'CPUParticles2D',
			'GPUParticles2D', 'Light2D', 'Line2D', 'Parallax2D', 'PathFollow2D', 'PointLight2D', 'Polygon2D', 'Sprite2D',
		],
	],
	[
		'3D',
		[
			'Node3D', 'GeometryInstance3D', 'SpriteBase3D', 'AnimatedSprite3D', 'Camera3D', 'CPUParticles3D', 'Decal',
			'FogVolume', 'GPUParticles3D', 'Label3D', 'Light3D', 'OmniLight3D', 'PathFollow3D', 'SpotLight3D', 'SpringArm3D',
		],
	],
	['UI', ['Control', 'Range', 'ColorRect', 'Label', 'RichTextLabel', 'ScrollContainer', 'TextureProgressBar']],
	['Materials', ['BaseMaterial3D']],
	['Animation and audio', ['AnimationPlayer', 'AudioStreamPlayer', 'AudioStreamPlayer2D', 'AudioStreamPlayer3D']],
	['Any node', ['Node']],
];

const byTarget = Map.groupBy(rows, (row) => row.target);
const known = new Set(SECTIONS.flatMap(([, targets]) => targets));
const others = [...byTarget.keys()].filter((t) => !known.has(t)).sort();
const ALL: [string, string[]][] = [...SECTIONS, ['Other', others]];

// Callback-only helpers target any Node; they read better under their own name. Anchors match Markdown heading ids.
const heading = (target: string) => (target === 'Node' ? 'Callback values' : target);
const anchor = (text: string) => text.toLowerCase().replace(/\s+/g, '-');

/** One page of the catalog, or the whole catalog without a section name. */
export function catalogSection(section?: string): CatalogSection {
	const targets = ALL.filter(([name]) => !section || name === section)
		.flatMap(([, names]) => names.filter((name) => byTarget.has(name)))
		.map((name) => ({ name, heading: heading(name), anchor: anchor(heading(name)), rows: byTarget.get(name)! }));
	return {
		targets,
		count: targets.reduce((n, t) => n + t.rows.length, 0),
		single: targets.length === 1,
		withProperty: targets.some((t) => t.rows.some((row) => row.property.length > 0)),
	};
}
