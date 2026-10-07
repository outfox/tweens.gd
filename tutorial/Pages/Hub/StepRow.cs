using Godot;

namespace tutorial;

/// <summary>One step of a language's path: number, title, what it teaches, and its API.</summary>
[Tool]
public partial class StepRow : CardButton
{
    private int number = 1;
    private Language track;

    [Export(PropertyHint.Range, "1,5")]
    public int Number
    {
        get => number;
        set
        {
            number = value;
            Fill();
        }
    }

    [Export]
    public Language Track
    {
        get => track;
        set
        {
            track = value;
            Fill();
        }
    }

    public override void _Ready()
    {
        Fill();
        base._Ready();
    }

    private void Fill()
    {
        if (!IsNodeReady() && !Engine.IsEditorHint() || GetNodeOrNull("%Title") is null)
            return;
        var step = Steps.All[number - 1];
        var first = number == 1;
        Normal = first ? "FirstStep" : "StepRow";
        GetNode<Dot>("%Dot").Number = number.ToString();
        GetNode<Dot>("%Dot").Filled = first;
        GetNode<Label>("%Title").Text = step.Label;
        GetNode<Label>("%Description").Text = step.Text(track);
        GetNode<Control>("%Begin").Visible = first;
        GetNode<Control>("%Arrow").Visible = !first;

        var api = GetNode<Container>("%Api");
        api.Visible = step.Api(track).Length > 0;
        foreach (var chip in api.GetChildren())
            chip.Free();
        foreach (var name in step.Api(track))
            api.AddChild(new Label { Text = name, ThemeTypeVariation = "ApiChip" });
        QueueRedraw();
    }

    protected override void HoverChanged(bool on)
        => GetNode<Label>("%Title").ThemeTypeVariation = on ? "StepTitleHover" : "StepTitle";

    public override void _Draw()
    {
        // The path line runs from this step's dot down to the next one's.
        if (number == Steps.All.Length)
            return;
        var dot = GetNode<Control>("%Dot");
        var x = dot.GetGlobalRect().GetCenter().X - GetGlobalRect().Position.X;
        var top = dot.GetGlobalRect().End.Y - GetGlobalRect().Position.Y;
        DrawRect(new Rect2(x - 1, top, 2, Size.Y - top + 24), GetThemeColor("outline", "Tutorial"));
    }
}
