using System.Globalization;
using System.Linq;
using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>
/// "Your first tween": the lesson's click-to-move scene on a stage of Godot's default window size. The panel sets the
/// script's properties; the call under the stage shows each start's endpoint, duration, and ease, as on the website.
/// </summary>
public partial class QuickstartPage : Page
{
    // Ghosts mark equal steps in time, so they bunch up where the icon is slow. Each lasts a second, then fades.
    private const int GhostSteps = 10;
    private const double GhostLife = 1;
    private const double GhostFade = 0.4;

    [Export] public PackedScene CSharpScene { get; set; } = null!;
    [Export] public PackedScene GDScriptScene { get; set; } = null!;

    private Stage stage = null!;
    private CodeView call = null!;
    private CodeView script = null!;
    private HSlider seconds = null!;
    private Button spin = null!;
    private Label pointer = null!;
    private Marker marker = null!;
    private Node2D ghosts = null!;
    private EaseChip[] chips = [];
    private Sprite2D? icon;

    private Vector2 from = new(288, 324);
    private Vector2 to = new(864, 324);
    private float fromRotation;
    private bool clicked;
    private double elapsed = -1;
    private double moveSeconds;
    private int dropped;

    private EaseChip Chosen => chips.First(chip => chip.ButtonPressed);

    public override void _Ready()
    {
        // Ghosts sample the icon after both tween runners (priority 1000) have moved it this frame.
        ProcessPriority = 1001;
        stage = GetNode<Stage>("%Stage");
        call = GetNode<CodeView>("%Call");
        script = GetNode<CodeView>("%Script");
        seconds = GetNode<HSlider>("%Seconds");
        spin = GetNode<Button>("%Spin");
        pointer = GetNode<Label>("%Pointer");
        chips = GetNode("%Chips").GetChildren().OfType<EaseChip>().ToArray();
        stage.Underlay.AddChild(ghosts = new Node2D { Name = "Ghosts" });
        stage.Underlay.AddChild(marker = new Marker { Visible = false });

        var amber = GetThemeColor("amber", "Tutorial");
        var blue = GetThemeColor("blue", "Tutorial");
        var mint = GetThemeColor("mint", "Tutorial");
        call.AddSlot("to", amber);
        call.AddSlot("seconds", blue);
        call.AddSlot("ease", mint);
        script.AddSlot("seconds", blue, @"Seconds \{ get; set; \} = ([\d.]+);", @"var seconds := ([\d.]+)");
        script.AddSlot("ease", mint, @"Ease \{ get; set; \} = ([\w.]+);", @"var easing := ([\w.]+)");
        script.AddMark(1, @"if \(@event is", null, @"if event is", null);
        script.AddMark(2, @"this\.TweenPosition", null, @"Tweens\.play\(self, Tweens\.position_2d", null);
        script.AddMark(3, @"\[Export\] public double", @"public EaseType Ease", @"@export var seconds", @"var easing");

        var view = stage.GetNode<Control>("View");
        view.GuiInput += OnStageInput;
        view.MouseExited += () => pointer.Visible = false;
        foreach (var chip in chips)
            chip.Pressed += () => Changed("ease");
        seconds.ValueChanged += _ => Changed("seconds", replay: false);
        seconds.DragEnded += changed =>
        {
            if (changed)
                Replay();
        };
        spin.Toggled += _ => Changed(null);
        GetNode<Button>("%Replay").Pressed += Replay;

        base._Ready();
        AutoPlay();
    }

    /// <summary>Plays the first move once the page has slid in, unless the reader clicked first.</summary>
    private async void AutoPlay()
    {
        if (await this.TweenFloat(0, 0.6).End == Reason.Completed && !clicked)
            Replay();
    }

    protected override void LanguageChanged(Language language)
    {
        // The twin lesson takes over where the icon stands.
        var at = icon?.Position;
        var rotation = icon?.Rotation ?? 0;
        Load();
        if (at is { } position)
        {
            icon!.Position = position;
            icon.Rotation = rotation;
        }
        elapsed = -1;
        ShowValues(flash: false);
    }

    private void Load()
    {
        var lesson = stage.Load(Languages.Pick(CSharpScene, GDScriptScene), lesson => Configure(lesson.GetNode("Icon")));
        icon = lesson.GetNode<Sprite2D>("Icon");
    }

    /// <summary>Sets the lesson script's properties, named as each language names them.</summary>
    private void Configure(Node target)
    {
        target.Set(Languages.Member("Seconds"), seconds.Value);
        target.Set(Languages.Member("Spin"), spin.ButtonPressed);
        target.Set(Languages.Pick("Ease", "easing"), Chosen.Ease);
    }

    private void Changed(string? slot, bool replay = true)
    {
        if (icon is not null)
            Configure(icon);
        ShowValues(flash: false);
        if (slot is not null)
        {
            call.SetSlot(slot, slot == "ease" ? EaseName() : Seconds());
            script.SetSlot(slot, slot == "ease" ? EaseName() : Seconds());
        }
        if (replay)
            Replay();
    }

    private string Seconds() => seconds.Value.ToString("0.0", CultureInfo.InvariantCulture);

    private string EaseName()
    {
        var chip = Chosen;
        var family = chip.Name.ToString();
        return $"{chip.GetMeta("leg")}.{(Languages.IsGDScript ? family.ToSnakeCase().ToUpperInvariant() : family)}";
    }

    private void ShowValues(bool flash)
    {
        GetNode<Label>("%Value").Text = $"{Seconds()} s";
        // Linear is the default, so the call omits the ease, as on the website.
        var ease = Chosen.Name == "Linear" ? "" : ", «ease:Out.Back»";
        call.ShowCode(
            "this.TweenPosition(«to:(864, 324)», «seconds:1.2»" + ease + ");" + (spin.ButtonPressed
                ? "\nthis.TweenRotation(Rotation + Mathf.Tau, «seconds:1.2», InOut.SmootherStep);" : ""),
            "Tweens.play(self, Tweens.position_2d(«to:[864, 324]», «seconds:1.2»" + ease + "))" + (spin.ButtonPressed
                ? "\nTweens.play(self, Tweens.rotation_2d(rotation + TAU, «seconds:1.2», InOut.SMOOTHER_STEP))" : ""));
        call.SetSlot("to", Endpoint(to), quiet: !flash);
        call.SetSlot("seconds", Seconds(), quiet: true);
        call.SetSlot("ease", EaseName(), quiet: true);
        script.SetSlot("seconds", Seconds(), quiet: true);
        script.SetSlot("ease", EaseName(), quiet: true);
    }

    private static string Endpoint(Vector2 point)
        => Languages.Pick($"({point.X:0}, {point.Y:0})", $"[{point.X:0}, {point.Y:0}]");

    private void OnStageInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
            ShowPointer(motion.Position);
        if (@event is not InputEventMouseButton { Pressed: true } press || press.ButtonIndex > MouseButton.Middle)
            return;
        // The lesson gets this click right after this handler, so the icon hasn't moved yet.
        Begin(stage.ToWorld(press.Position).Round());
        if (!clicked)
        {
            clicked = true;
            GetNode<CanvasItem>("%Hint").TweenModulateAlpha(0, 0.4, Out.Quad);
        }
    }

    private void ShowPointer(Vector2 local)
    {
        var at = stage.ToWorld(local).Round();
        pointer.Text = $"({at.X:0}, {at.Y:0})";
        pointer.Size = pointer.GetMinimumSize();
        var flip = local.X > stage.Size.X - pointer.Size.X - 24;
        pointer.Position = local + (flip ? new Vector2(-pointer.Size.X - 10, 14) : new Vector2(14, 14));
        pointer.Visible = true;
    }

    /// <summary>Records a move as it starts, from wherever the icon stands.</summary>
    private void Begin(Vector2 target)
    {
        if (icon is null)
            return;
        from = icon.Position;
        fromRotation = icon.Rotation;
        to = target;
        marker.Pop(to);
        call.SetSlot("to", Endpoint(to));
        ClearGhosts();
        elapsed = 0;
        dropped = 0;
        moveSeconds = seconds.Value;
    }

    /// <summary>Plays the last move again from its start, on a fresh copy of the lesson scene.</summary>
    private void Replay()
    {
        var start = from;
        var rotation = fromRotation;
        Load();
        icon!.Position = start;
        icon.Rotation = rotation;
        Begin(to);
        stage.Click(to);
    }

    public override void _Process(double delta)
    {
        if (elapsed < 0 || icon is null)
            return;
        elapsed += delta;
        var due = Mathf.Min((int)(elapsed / moveSeconds * GhostSteps), GhostSteps - 1);
        for (; dropped <= due; dropped++)
            AddGhost(elapsed - dropped * moveSeconds / GhostSteps);
        if (dropped == GhostSteps)
            elapsed = -1;
    }

    private async void AddGhost(double age)
    {
        var ghost = new Sprite2D
        {
            Texture = icon!.Texture,
            Position = icon.Position,
            Rotation = icon.Rotation,
            Modulate = Colors.White with { A = 0.2f },
        };
        ghosts.AddChild(ghost);
        // A ghost shows for a second from when the icon passed its spot, then fades.
        var fade = ghost.TweenModulateAlpha(0, GhostFade, Out.Quad, Mathf.Max(0, GhostLife - age));
        if (await fade.End == Reason.Completed)
            ghost.QueueFree();
    }

    private void ClearGhosts()
    {
        foreach (var ghost in ghosts.GetChildren().OfType<Sprite2D>())
        {
            ghost.CancelTweens();
            FadeOut(ghost);
        }
    }

    private static async void FadeOut(Sprite2D ghost)
    {
        if (await ghost.TweenModulateAlpha(0, 0.35).End == Reason.Completed)
            ghost.QueueFree();
    }
}
