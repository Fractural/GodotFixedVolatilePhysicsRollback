using Godot;
using Godot.Collections;
using GodotRollbackNetcode;
using Volatile.GodotEngine;

namespace Game
{
    [Tool]
    public class NetworkVolatileWorld : VolatileWorld, INetworkProcess
    {
        public override void _Ready()
        {
            ProcessSelf = false;
            base._Ready();
        }

        public void _NetworkProcess(Dictionary input)
        {
            World.Update();
        }
    }
}