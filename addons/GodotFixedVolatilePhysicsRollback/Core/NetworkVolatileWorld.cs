using Godot;
using Godot.Collections;
using GodotRollbackNetcode;

namespace Volatile.GodotEngine.Rollback
{
    [Tool]
    public class NetworkVolatileWorld : VolatileWorld, INetworkProcess
    {
        public override void _Ready()
        {
            base._Ready();

            ProcessSelf = false;
        }

        public void _NetworkProcess(Dictionary input)
        {
            World.Update();
            Update();
        }
    }
}