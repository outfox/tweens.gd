using Godot;
using tweens.gd;
public partial class Consumer : Node2D
{
    private int frames;
    public override void _Process(double delta)
    {
        if (++frames > 300) SetMeta("smoke_finished", true);
    }
    public override async void _Ready()
    {
        try
        {
            await Animate();
            if (Position != new Vector2(100, 50) || !Mathf.IsEqualApprox(Rotation, 1))
                throw new System.Exception("Unexpected final tween values.");
            GD.Print("C# addon smoke passed.");
            SetMeta("smoke_passed", true);
            SetMeta("smoke_finished", true);
        }
        catch (System.Exception error) { GD.PushError(error.ToString()); SetMeta("smoke_finished", true); }
    }
    public async System.Threading.Tasks.Task Animate()
    {
        var move = new Tweens.Position2D(new Vector2(100, 50), 0.25);
        var custom = new Tweens.Property<Node2D, float>(
            node => node.Rotation, (node, value) => node.Rotation = value,
            Interpolators.Float) { To = 1, Duration = 0.1 };
        await this.Chain([move with { Delay = 0.1 }, custom]).End;
    }
}
