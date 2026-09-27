// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System;
using System.Threading.Tasks;
using Godot;

namespace testbed;

/// <summary>Runs the same GDScript source that the gallery displays.</summary>
public sealed class GalleryGDScript : IDisposable
{
    private readonly GDScript script;
    private readonly GodotObject animation;
    private readonly TaskCompletionSource completion = new();
    public Task Completion => completion.Task;

    public GalleryGDScript(GallerySource source)
    {
        // Compile the embedded source, so exported builds cannot display a different version.
        script = new GDScript { SourceCode = source.Text };
        var error = script.Reload();
        if (error != Error.Ok || !script.CanInstantiate())
        {
            script.Dispose();
            throw new InvalidOperationException($"Cannot compile {source.Path}: {error}");
        }
        animation = script.New().AsGodotObject();
        animation.Connect("finished", Callable.From(() => completion.TrySetResult()), (uint)GodotObject.ConnectFlags.OneShot);
    }

    public void Start(Control stage, Godot.Collections.Dictionary targets, double seconds) =>
        animation.Call("start", stage, targets, seconds);

    public void Dispose()
    {
        // Page teardown has already cancelled all node-owned playback before resource release.
        animation.Dispose();
        script.Dispose();
    }
}
