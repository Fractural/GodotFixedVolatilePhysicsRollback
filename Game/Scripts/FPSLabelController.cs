using Godot;

namespace Game
{
    public class FPSLabelController : Label
    {
        public override void _Process(float delta)
        {
            Text = $"FPS: {Engine.GetFramesPerSecond()}";
        }
    }
}