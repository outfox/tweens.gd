---
title: Groups
description: The members of Group, which controls and awaits several tweens as one step.
---

A `Group` controls several tweens as one step. `node.Tween(first, second, ...)`
returns one, and `Group.Of` groups tweens that are already playing. It has the
control and await members of a [handle](/csharp/api/handles/), but no `State` or
`Progress`.

## Create a group

| Member | Type | Meaning |
| --- | --- | --- |
| `Group.Of(tweens)` | `Group` | Group tweens that are already playing |
| `Members` | `IReadOnlyList<TweenInstance>` | The grouped handles |

## Control the group

| Member | Type | Meaning |
| --- | --- | --- |
| `Pause()`, `Resume()` | `void` | Pause or resume every member |
| `IsPaused` | `bool` | True only while every active member is paused |
| `Cancel()` | `void` | Cancel every member that is still playing |

## Await the group

| Member | Type | Meaning |
| --- | --- | --- |
| `End` | `Task<Reason>` | Completes when every member completes |
| `IsTerminal` | `bool` | True once the group has ended |
| `CompletionReason` | `Reason?` | `Completed`, or the reason of the first member that stopped early |
| `Error` | `Exception?` | The exception that faulted a member, if one did |

If one member stops early, the group cancels the others and reports that member's
reason, or faults. See [sequences](/csharp/sequences/).
