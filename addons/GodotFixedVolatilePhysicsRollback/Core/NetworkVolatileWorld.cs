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
            AddToGroup("network_sync");
            ProcessSelf = false;
            SetPhysicsProcess(false);
            base._Ready();
        }

        public void _NetworkProcess(Dictionary input)
        {
            foreach (var body in World.Bodies)
            {
                if (body.IsDynamic)
                {
                    var dynamicBody = body;
                }
            }
            World.Update();
            Update();
            foreach (var body in World.Bodies)
            {
                if (body.IsDynamic)
                {
                    var dynamicBody = body;
                }
            }
        }
    }
}