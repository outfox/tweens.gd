---
title: Groups
description: Group, which pauses, cancels, and awaits several C# tweens as one step.
tableOfContents: true
---

A `Group` treats several tweens as one step: it pauses, cancels, and completes them together. [Sequences](/csharp/sequences/#play-together) introduces groups.

```csharp
var step = Group.Of(sprite.TweenPositionX(300, 0.6), label.TweenModulateAlpha(0, 0.6));
await step.End;
```

## Create

| Member | Type | Meaning |
| --- | --- | --- |
| `Group.Of(tweens)` | `Group` | Group running handles, on any targets |
| `node.Tween(first, second)` | `Group` | Start several definitions on one node as a group; see [starting playback](/csharp/api/start/#on-a-node) |

## Control

| Member | Type | Meaning |
| --- | --- | --- |
| `Pause()` | `void` | Pause every member |
| `Resume()` | `void` | Resume every member |
| `IsPaused` | `bool` | True while every active member is paused |
| `Cancel()` | `void` | Cancel every member still playing |

## Status

| Member | Type | Meaning |
| --- | --- | --- |
| `Members` | `IReadOnlyList<TweenInstance>` | The grouped handles |
| `IsTerminal` | `bool` | True once the group has ended |
| `CompletionReason` | `Reason?` | `Completed`, or the reason of the first member that stopped early; `null` until the group ends |
| `Error` | `Exception?` | The exception that faulted a member, if one did |

## Awaiting

| Member | Type | Meaning |
| --- | --- | --- |
| `End` | `Task<Reason>` | Completes when every member has ended; faults when a member faults |

## Rules

- If one member stops early or faults, the group cancels the others and reports that member's reason or exception.
- A group has no `State` or `Progress`; read its members for those.
- A group doesn't link timing: each member keeps its own start and delay. A [Chain](/csharp/api/chains/) plays definitions in order.

<!-- Keep links to earlier sections working. -->
<span id="create-a-group"></span>
<span id="control-the-group"></span>
<span id="await-the-group"></span>
