using FixMath.NET;
using Godot;
using Godot.Collections;
using GodotRollbackNetcode;

namespace Volatile.GodotEngine.Rollback
{
    [Tool]
    public class NetworkVoltNode2D : VoltNode2D, INetworkSpawn, INetworkSerializable
    {
        #region Network
        public const string SPAWN_POSITION = "position";
        public const string SPAWN_ROTATION = "rotation";
        public const string SPAWN_SCALE = "scale";
        public const string SPAWN_TRANSFORM = "transform";

        public const string STATE_TRANSFORM = "transform";

        public override void _EnterTree()
        {
            base._EnterTree();
            AddToGroup("network_sync");
        }

        public virtual void _NetworkSpawn(Dictionary data)
        {
            if (data.Contains(SPAWN_TRANSFORM))
                FixedTransform = data.GetVoltDeserialized<VoltTransform2D>(SPAWN_TRANSFORM);
            else
            {
                if (data.Contains(SPAWN_POSITION))
                    GlobalFixedPosition = data.GetVoltDeserialized<VoltVector2>(SPAWN_POSITION);
                if (data.Contains(SPAWN_ROTATION))
                    GlobalFixedRotation = data.GetVoltDeserialized<Fix64>(SPAWN_ROTATION);
                if (data.Contains(SPAWN_SCALE))
                    FixedScale = data.GetVoltDeserialized<VoltVector2>(SPAWN_SCALE);
            }
        }

        public virtual Dictionary _SaveState()
        {
            return new Dictionary()
            {
                [STATE_TRANSFORM] = VoltType.Serialize(FixedTransform)
            };
        }

        public virtual void _LoadState(Dictionary state)
        {
            FixedTransform = state.GetVoltDeserialized<VoltTransform2D>(STATE_TRANSFORM);
        }
        #endregion
    }
}