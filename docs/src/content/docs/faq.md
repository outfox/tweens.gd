---
title: Frequently asked questions
description: Why tweens.gd exists next to Godot's built-in Tween.
---

## Why not use Godot's built-in Tween?

You tune the same motions all through a project. The shop panel should open a little
faster, and the coin pop overshoots too far. With Godot's `Tween`, those durations and
eases live in every script that animates something, so each playtest note turns into a
search through the project, and whatever you miss now feels different.

With tweens.gd, you define a motion once and start it wherever you need it. Change the
definition, and every future start uses the change. Where one spot needs a bigger or
slower version, a variation scales the shared definition instead of copying it
([C#](/csharp/variations/), [GDScript](/gdscript/variations/)). Properties aren't spelled as strings, so a typo fails at compile time in
C#, or when the tween starts in GDScript. When an enemy dies mid-animation, its tweens
stop and your awaiting code learns how they ended.
