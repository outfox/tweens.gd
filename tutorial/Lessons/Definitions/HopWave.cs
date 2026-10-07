using System.Linq;
using Godot;
using tweens.gd;

public partial class HopWave : Node2D
{
    [Export] public float Height { get; set; } = 80;
    [Export] public double Stagger { get; set; } = 0.2;

    public override void _Ready()
    {
        var hop = new Tweens.Position2DY
        {
            By = -Height,
            Duration = 0.25,
            Ease = Out.Quad,
            PingPong = true,
        };

        var icons = GetChildren().OfType<Node2D>().ToArray();
        for (var i = 0; i < icons.Length; i++)
            icons[i].Tween(hop with { Delay = i * Stagger });
    }
}
