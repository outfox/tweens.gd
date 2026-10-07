using Godot;
using twodog.Testing;

namespace tutorial.Tests;

/// <summary>Headless Godot at a fixed 60 frames per second, so tweens advance exactly 1/60 s per frame.</summary>
public sealed class TutorialFixture() : FixtureBase("--headless", "--fixed-fps", "60")
{
    public void Frames(int count)
    {
        for (var i = 0; i < count; i++)
            Engine.Iteration();
    }

    public void Seconds(double seconds) => Frames((int)Math.Ceiling(seconds * 60));
}

[CollectionDefinition(nameof(TutorialCollection), DisableParallelization = true)]
public sealed class TutorialCollection : ICollectionFixture<TutorialFixture>;

/// <summary>Hosts a page or lesson under the root and frees it afterwards.</summary>
public sealed class Hosted<T> : IDisposable where T : Node
{
    public Hosted(TutorialFixture godot, T node)
    {
        Node = node;
        godot.Tree.Root.AddChild(node);
        godot.Frames(2);
    }

    public T Node { get; }

    public void Dispose()
    {
        if (GodotObject.IsInstanceValid(Node))
            Node.Free();
    }
}
