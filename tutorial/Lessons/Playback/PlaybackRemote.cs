using Godot;
using tweens.gd;

// The remote's buttons call these methods; Ended reports how the movement ended.
public partial class PlaybackRemote : Node2D
{
    [Signal]
    public delegate void EndedEventHandler(Reason reason);

    [Export] private Sprite2D sprite = null!;
    [Export] private Vector2 to = new(1024, 128);

    private TweenInstance? movement;

    public async void Start()
    {
        movement = sprite.TweenPosition(to, 2.4, InOut.SmootherStep);
        EmitSignalEnded(await movement.End);
    }

    public void Pause() => movement?.Pause();

    public void Resume() => movement?.Resume();

    public void Cancel() => movement?.Cancel();
}
