// Demos are shared by both language paths; these map the C# names they are written with to GDScript's.

export type Lang = 'csharp' | 'gdscript';

/** PingPongInterval → ping_pong_interval */
export const snake = (name: string) => name.replace(/([a-z0-9])([A-Z])/g, '$1_$2').toLowerCase();

/** BackInOut → BACK_IN_OUT */
export const constant = (name: string) => snake(name).toUpperCase();

/** A timing or option property: `Delay` in C#, `delay` in GDScript. */
export const field = (lang: Lang, name: string) => (lang === 'gdscript' ? snake(name) : name);

/** An enum member: `EaseType.BackOut` in C#, `Tweens.Ease.BACK_OUT` in GDScript. */
export const member = (lang: Lang, csharpEnum: string, gdscriptEnum: string, name: string) =>
	lang === 'gdscript' ? `Tweens.${gdscriptEnum}.${constant(name)}` : `${csharpEnum}.${name}`;

/** The language a demo was rendered for, from its root's data-code-lang. */
export const langOf = (root: HTMLElement): Lang => (root.dataset.codeLang === 'gdscript' ? 'gdscript' : 'csharp');
