using Godot;

namespace tutorial;

/// <summary>Links a step to its neighbors, like the website's pagination.</summary>
[GlobalClass]
public partial class StepFooter : HBoxContainer
{
    public override void _Ready()
    {
        var page = GetOwner<Page>();
        var previous = GetNode<CardButton>("%Previous");
        var next = GetNode<CardButton>("%Next");
        var step = page.Step;

        GetNode<Label>("%PreviousTitle").Text = step > 1 ? Steps.All[step - 2].Label : "Tutorial";
        previous.Pressed += () => page.Navigate?.Invoke(step - 1);
        if (step < Steps.All.Length)
        {
            GetNode<Label>("%NextTitle").Text = Steps.All[step].Label;
            next.Pressed += () => page.Navigate?.Invoke(step + 1);
        }
        else
        {
            GetNode<Label>("%NextCaption").Text = "Finished";
            GetNode<Label>("%NextTitle").Text = "Back to the overview";
            next.Pressed += () => page.Navigate?.Invoke(0);
        }
    }
}
