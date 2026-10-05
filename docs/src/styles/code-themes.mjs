// Syntax colors for code blocks, one pair per language: C# follows JetBrains Rider's Islands themes, GDScript the Godot
// editor's script themes. Code backgrounds come from the site palette (theme.css); `bg` is the page stage, which file
// tabs show. theme.css mirrors these colors for the hand-highlighted code in demos.
import { ExpressiveCodeTheme } from '@astrojs/starlight/expressive-code';

const theme = (name, type, c, tokenColors) =>
	new ExpressiveCodeTheme({
		name,
		type,
		colors: { 'editor.background': c.bg, 'editor.foreground': c.fg },
		tokenColors: [
			{ scope: ['comment', 'punctuation.definition.comment'], settings: { foreground: c.comment } },
			{ scope: ['punctuation', 'keyword.operator', 'meta.brace'], settings: { foreground: c.punctuation } },
			{ scope: ['constant.numeric'], settings: { foreground: c.number } },
			{ scope: ['string', 'punctuation.definition.string', 'constant.character'], settings: { foreground: c.string } },
			{ scope: ['markup.inserted'], settings: { foreground: c.inserted } },
			{ scope: ['markup.deleted'], settings: { foreground: c.deleted } },
			...tokenColors.map(([scope, foreground]) => ({ scope, settings: { foreground } })),
		],
	});

const csharp = (name, type, c) =>
	theme(name, type, c, [
		[
			[
				'keyword',
				'storage.modifier',
				'storage.type',
				'keyword.type',
				'keyword.operator.expression',
				'keyword.operator.new',
				'constant.language',
				'variable.language',
			],
			c.keyword,
		],
		[['entity.name.type', 'entity.other.inherited-class', 'support.type', 'support.class'], c.type],
		[['entity.name.function', 'support.function'], c.method],
		[['variable.other.object.property', 'entity.name.variable.property', 'variable.other.property'], c.property],
		[['meta.embedded.interpolation punctuation.definition.interpolation'], c.keyword],
	]);

const gdscript = (name, type, c) =>
	theme(name, type, c, [
		[['keyword', 'storage.type', 'keyword.operator.wordlike', 'constant.language.literal'], c.keyword],
		[['keyword.control'], c.control],
		[['entity.name.type.class.builtin'], c.baseType],
		[['entity.name.type', 'support.type', 'support.class'], c.engineType],
		[['entity.name.function'], c.function],
		[['meta.function entity.name.function'], c.functionDefinition],
		[['entity.name.function.decorator'], c.annotation],
		[['constant.language', 'variable.other.property'], c.member],
		[['meta.literal.nodepath', 'meta.literal.nodepath keyword.control.flow'], c.nodePath],
		[['variable.parameter', 'variable.other'], c.fg],
	]);

export const csharpDark = csharp('csharp-dark', 'dark', {
	bg: '#111b27',
	fg: '#bcbec4',
	comment: '#85c46c',
	punctuation: '#bcbec4',
	keyword: '#6c95eb',
	type: '#c191ff',
	method: '#39cc9b',
	property: '#66c3cc',
	number: '#ed94c0',
	string: '#c9a26d',
	inserted: '#39cc9b',
	deleted: '#ed94c0',
});

export const csharpLight = csharp('csharp-light', 'light', {
	bg: '#e9eff6',
	fg: '#1e1f22',
	comment: '#3f7a2c',
	punctuation: '#1e1f22',
	keyword: '#1849c2',
	type: '#7036b5',
	method: '#08735a',
	property: '#0b6b80',
	number: '#b0226b',
	string: '#8a5410',
	inserted: '#08735a',
	deleted: '#b0226b',
});

export const gdscriptDark = gdscript('gdscript-dark', 'dark', {
	bg: '#111b27',
	fg: '#cdcfd2',
	comment: '#8a9196',
	punctuation: '#abc9ff',
	keyword: '#ff7085',
	control: '#ff8ccc',
	baseType: '#42ffc2',
	engineType: '#8fffdb',
	function: '#57b3ff',
	functionDefinition: '#66e6ff',
	member: '#bce0ff',
	annotation: '#ffb373',
	nodePath: '#b8c47d',
	number: '#a1ffe0',
	string: '#ffeda1',
	inserted: '#42ffc2',
	deleted: '#ff7085',
});

export const gdscriptLight = gdscript('gdscript-light', 'light', {
	bg: '#e9eff6',
	fg: '#1f2329',
	comment: '#5f666d',
	punctuation: '#29408f',
	keyword: '#c4155a',
	control: '#ad1a8a',
	baseType: '#08774f',
	engineType: '#11705a',
	function: '#1b4fc4',
	functionDefinition: '#0a6680',
	member: '#0c5d94',
	annotation: '#9c4a00',
	nodePath: '#5e6b12',
	number: '#08735a',
	string: '#855b00',
	inserted: '#08774f',
	deleted: '#c4155a',
});

// GDScript themes apply to GDScript blocks, and to blocks in other languages while the reader follows GDScript.
// The selector lands on the code block itself; its root form, which Expressive Code also emits, matches nothing.
export const codeThemeSelector = ({ name, type }) => {
	const root = `:root[data-theme='${type}']`;
	if (name.startsWith('csharp')) return `[data-theme='${type}']`;
	return (
		`:is(${root}[data-lang='gdscript'] .expressive-code:not(:has(pre[data-language='csharp'])), ` +
		`${root} .expressive-code:has(pre[data-language='gdscript']))`
	);
};
