---
title: Frequently asked questions
description: Why tweens.gd exists next to Godot's built-in Tween.
---

## But... why?

You tune the same motions all through a project. The shop panel should open a little
faster, and the coin pop overshoots too far. With `create_tween()`, those durations and
eases live in every script that animates something, so each playtest note turns into a
search through the project, and whatever you miss now feels different.

With tweens.gd, you define a motion once and start it wherever you need it. Change the
definition, and every future start uses the change. You don't spell properties as
strings, so a typo gets caught early, and when an enemy dies mid-animation, its tweens
stop and your awaiting code learns how they ended.
