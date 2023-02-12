using Godot;
using GDC = Godot.Collections;

namespace Game
{
    public class TouchControls : Control
    {
        [Export]
        public string Prefix = "player1_";

        [Export]
        NodePath upButtonPath;
        [Export]
        NodePath downButtonPath;
        [Export]
        NodePath leftButtonPath;
        [Export]
        NodePath rightButtonPath;
        [Export]
        NodePath bombButtonPath;
        [Export]
        NodePath teleportButtonPath;

        public override void _Ready()
        {
            bool mobile = OS.GetName() == "Android" || OS.GetName() == "iOS";
            Visible = mobile;
            SetProcess(Visible);

            BindActionButton(upButtonPath, "up");
            BindActionButton(downButtonPath, "down");
            BindActionButton(leftButtonPath, "left");
            BindActionButton(rightButtonPath, "right");
            BindActionButton(bombButtonPath, "bomb");
            BindActionButton(teleportButtonPath, "teleport");

        }

        private void BindActionButton(NodePath buttonPath, string action)
        {
            BindActionButton(GetNode<Button>(buttonPath), action);
        }



        private void BindActionButton(Button button, string action)
        {
            button.Connect("button_down", this, nameof(OnButtonDown), new GDC.Array(new string[] { action }));
            button.Connect("button_up", this, nameof(OnButtonUp), new GDC.Array(new string[] { action }));
        }

        private void OnButtonUp(string buttonAction)
        {
            GD.Print($"{Prefix + buttonAction} action released");
            Input.ActionRelease(Prefix + buttonAction);
        }

        private void OnButtonDown(string buttonAction)
        {
            GD.Print($"{Prefix + buttonAction} action pressed");
            Input.ActionPress(Prefix + buttonAction);
        }
    }
}