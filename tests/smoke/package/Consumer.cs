// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using Godot;
using tweens.gd;
public static class Consumer
{
    private static readonly Tweens.Position2D movement = new()
    {
        To = new Vector2(100, 50),
        Duration = 0.25,
    };

    public static System.Threading.Tasks.Task<Reason> Animate(Node2D node) =>
        node.Tween(movement with { Delay = 0.1 }).End;
}
