---
title: Acknowledgements
description: The project that inspired tweens.gd, and the notices both packages ship.
---

tweens.gd was inspired by [unity-tweens](https://github.com/jeffreylanters/unity-tweens)
by Jeffrey Lanters, a tweening library for Unity. Its model, a reusable tween definition
that each start copies into an independent instance, shaped the first design of this
library.

## Inspiration, not a port

tweens.gd has since been rewritten around Godot. Definitions target Godot nodes,
resources, and shader uniforms; playback follows node ownership and the scene tree's
pause and process modes. Await the handle directly in C#, or its `end` in
GDScript. The timing, fill, repeat, and completion rules are specified and tested
independently of unity-tweens, and the two libraries no longer share an API.

## Notices

The easing functions still follow unity-tweens' implementation. The C# package and the
GDScript addon therefore both include the unity-tweens MIT license in their
`THIRD-PARTY-NOTICES.md` files. Keep that file with the addon when you copy
`addons/tweens_gd/` into a project.
