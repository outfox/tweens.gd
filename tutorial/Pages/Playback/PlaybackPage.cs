using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>
/// "Control and completion": a remote for one playback handle. Each button calls the lesson's method of the same
/// name; the lesson reports how its await ended, and the code above flashes the line each action stands for.
/// </summary>
public partial class PlaybackPage : Page
{
    private const double Seconds = 2.4;

    private enum State
    {
        Ready,
        Playing,
        Paused,
        Completed,
        Cancelled,
    }

    [Export] public PackedScene CSharpScene { get; set; } = null!;
    [Export] public PackedScene GDScriptScene { get; set; } = null!;

    private Stage stage = null!;
    private CodeView remote = null!;
    private CodeView script = null!;
    private RemoteLane lane = null!;
    private PauseBadge badge = null!;
    private Button start = null!;
    private Button pause = null!;
    private Button resume = null!;
    private Button cancel = null!;
    private PanelContainer chip = null!;
    private Node? lesson;
    private Sprite2D? sprite;
    private State state;
    private double elapsed;

    public override void _Ready()
    {
        stage = GetNode<Stage>("%Stage");
        remote = GetNode<CodeView>("%Remote");
        script = GetNode<CodeView>("%Script");
        start = GetNode<Button>("%Start");
        pause = GetNode<Button>("%Pause");
        resume = GetNode<Button>("%Resume");
        cancel = GetNode<Button>("%Cancel");
        chip = GetNode<PanelContainer>("%State");
        stage.Underlay.AddChild(lane = new RemoteLane { Name = "Lane" });
        stage.Overlay.AddChild(badge = new PauseBadge { Name = "Badge", Visible = false });

        remote.AddSlot("reason", GetThemeColor("mint", "Tutorial"));
        script.AddMark(1, @"movement = sprite\.TweenPosition", null, @"movement = Tweens\.play", null);
        script.AddMark(2, @"EmitSignalEnded\(await", null, @"ended\.emit\(await", null);
        script.AddMark(3, @"public void Pause", @"public void Cancel", @"func pause", @"movement\.cancel");

        start.Pressed += Start;
        pause.Pressed += () => Act("Pause", State.Paused);
        resume.Pressed += () => Act("Resume", State.Playing);
        cancel.Pressed += () => Act("Cancel", null);
        base._Ready();
    }

    protected override void LanguageChanged(Language language)
    {
        remote.ShowCode(
            """
            var movement = sprite.TweenPosition(to, 2.4, InOut.SmootherStep);
            movement.Pause();
            movement.Resume();
            movement.Cancel();
            await movement.End; // «reason:pending…»
            """,
            """
            var movement := Tweens.play(sprite, Tweens.position_2d(to, 2.4, InOut.SMOOTHER_STEP))
            movement.pause()
            movement.resume()
            movement.cancel()
            await movement.end # «reason:pending…»
            """);
        GetNode<CodeView>("%Await").ShowCode(
            """
            var movement = sprite.TweenPosition((400, 180), 0.6);
            await movement.End;
            """,
            """
            var movement := Tweens.play(sprite, Tweens.position_2d([400, 180], 0.6))
            await movement.end
            """);
        GetNode<CodeView>("%Callbacks").ShowCode(
            """
            var arrive = new Tweens.Position2D((400, 180), 0.6, Out.Cubic)
            {
                OnEnd = _ => GD.Print("Arrived"),
            };
            sprite.Tween(arrive);
            """,
            """
            var arrive := Tweens.position_2d([400, 180], 0.6, Out.CUBIC) \
                .with_on_end(func(_handle): print("Arrived"))
            Tweens.play(sprite, arrive)
            """);
        Load();
    }

    /// <summary>A fresh copy of the lesson scene, with its sprite back at the start.</summary>
    private void Load()
    {
        lesson = stage.Load(Languages.Pick(CSharpScene, GDScriptScene));
        lesson.Connect(Languages.Member("Ended"), Callable.From((int reason) => Ended((Reason)reason)));
        sprite = lesson.GetNode<Sprite2D>("Sprite");
        lane.Sprite = badge.Sprite = sprite;
        lane.From = sprite.Position;
        lane.To = lesson.Get("to").AsVector2();
        elapsed = 0;
        remote.SetSlot("reason", "pending…", quiet: true);
        SetState(State.Ready);
    }

    private void Start()
    {
        if (state != State.Ready)
            Load();
        remote.FlashLine("TweenPosition", "Tweens.play");
        lesson!.Call(Languages.Member("Start"));
        SetState(State.Playing);
    }

    private void Act(string method, State? next)
    {
        remote.FlashLine($@"\.{method}\(", $@"\.{method.ToLowerInvariant()}\(");
        lesson!.Call(Languages.Member(method));
        if (next is { } state)
            SetState(state);
    }

    /// <summary>The lesson's await resumed: show how the movement ended.</summary>
    private void Ended(Reason reason)
    {
        if (reason is not (Reason.Completed or Reason.Cancelled))
            return;
        remote.FlashLine(@"await", "await");
        remote.SetSlot("reason", Languages.IsGDScript
            ? $"Tweens.Reason.{reason.ToString().ToSnakeCase().ToUpperInvariant()}"
            : $"Reason.{reason}");
        SetState(reason == Reason.Completed ? State.Completed : State.Cancelled);
    }

    private void SetState(State next)
    {
        state = next;
        pause.Disabled = next != State.Playing;
        resume.Disabled = next != State.Paused;
        cancel.Disabled = next is not (State.Playing or State.Paused);
        start.Text = next == State.Ready ? "Start" : "Start again";
        badge.Display(next == State.Paused);
        lane.Arrived = next == State.Completed;
        if (sprite is not null)
            sprite.SelfModulate = next == State.Cancelled ? new Color(0.7f, 0.7f, 0.7f, 0.6f) : Colors.White;

        var label = chip.GetNode<Label>("Text");
        label.Text = next.ToString();
        var (fill, ink) = next switch
        {
            State.Playing => ("blue", "#0b1224"),
            State.Paused => ("amber", "#231705"),
            State.Completed => ("mint", "#0b1a17"),
            State.Cancelled => ("rose", "#2a0710"),
            _ => ("raised", "#eef4fa"),
        };
        var style = (StyleBoxFlat)chip.GetThemeStylebox("panel").Duplicate();
        style.BgColor = GetThemeColor(fill, "Tutorial");
        chip.AddThemeStyleboxOverride("panel", style);
        label.AddThemeColorOverride("font_color", new Color(ink));
        chip.OffsetTransformEnabled = true;
        chip.OffsetTransformPivotRatio = new Vector2(0.5f, 0.5f);
        chip.OffsetTransformScale = new Vector2(0.75f, 0.75f);
        chip.TweenOffsetTransformScale(Vector2.One, 0.42, Out.Back);
    }

    public override void _Process(double delta)
    {
        // The handle's own clock pauses with it; this readout follows the same rule.
        if (state == State.Playing)
            elapsed = Mathf.Min(Seconds, elapsed + delta);
        GetNode<Label>("%Progress").Text = (elapsed / Seconds).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }
}
