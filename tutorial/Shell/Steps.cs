namespace tutorial;

/// <summary>One tutorial step: its page, what a reader can do after it, and the API it introduces.</summary>
public sealed record Step(string Label, string Scene, string CSharpText, string GDScriptText,
    string[] CSharpApi, string[] GDScriptApi)
{
    public string Text(Language language) => language == Language.GDScript ? GDScriptText : CSharpText;
    public string[] Api(Language language) => language == Language.GDScript ? GDScriptApi : CSharpApi;
}

/// <summary>The five steps, in the website's order and words.</summary>
public static class Steps
{
    public const string Hub = "res://Pages/Hub/Hub.tscn";

    public static readonly Step[] All =
    [
        new("Install", "res://Pages/Install/InstallPage.tscn",
            "Add tweens.gd from the Godot Store or NuGet.",
            "Add tweens.gd from the Godot Store or GitHub Releases.",
            [], []),
        new("Your first tween", "res://Pages/Quickstart/QuickstartPage.tscn",
            "Send the Godot icon wherever you click.",
            "Send the Godot icon wherever you click.",
            ["this.TweenPosition()", "Out.Back"], ["Tweens.play()", "Tweens.position_2d()", "Out.BACK"]),
        new("Definitions", "res://Pages/Definitions/DefinitionsPage.tscn",
            "Define a motion once and start a copy on every node.",
            "Define a motion once and start a copy on every node.",
            ["Tweens.Position2DY", "with { }"], ["Tweens.position_2d_y()", "with_delay()"]),
        new("Syntax & Sugar", "res://Pages/SyntaxSugar/SyntaxSugarPage.tscn",
            "Choose structured definitions or convenient calls, with safe or sweet endpoints.",
            "Choose structured definitions or convenient calls, with safe or sweet endpoints.",
            ["Vector2", "(x, y)", "[x, y]"], ["Vector2", "[x, y]"]),
        new("Control & completion", "res://Pages/Playback/PlaybackPage.tscn",
            "Pause or cancel a tween, and react to how it ended.",
            "Pause or cancel a tween, and react to how it ended.",
            ["OnEnd", "CancelTweens()"], ["with_on_end()", "Tweens.cancel_tweens()"]),
    ];
}
