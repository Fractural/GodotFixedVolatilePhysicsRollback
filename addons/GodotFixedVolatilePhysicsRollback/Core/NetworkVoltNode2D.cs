using FixMath.NET;
using Godot;
using Godot.Collections;
using GodotRollbackNetcode;

namespace Volatile.GodotEngine.Rollback
{
    public class NetworkVoltNode2D : VoltNode2D, INetworkSpawn, INetworkSerializable
    {
        public const string SPAWN_POSITION = "position";
        public const string SPAWN_ROTATION = "rotation";
        public const string SPAWN_SCALE = "scale";
        public const string SPAWN_TRANSFORM = "transform";

        public const string STATE_TRANSFORM = "transform";

        public virtual void _NetworkSpawn(Dictionary data)
        {
            if (data.Contains(SPAWN_TRANSFORM))
                FixedTransform = data.GetVoltDeserialized<VoltTransform2D>(SPAWN_TRANSFORM);
            else
            {
                if (data.Contains(SPAWN_POSITION))
                    FixedPosition = data.GetVoltDeserialized<VoltVector2>(SPAWN_ROTATION);
                if (data.Contains(SPAWN_ROTATION))
                    FixedRotation = data.GetVoltDeserialized<Fix64>(SPAWN_ROTATION);
                if (data.Contains(SPAWN_SCALE))
                    FixedScale = data.GetVoltDeserialized<VoltVector2>(SPAWN_SCALE);
            }
        }

        public virtual Dictionary _SaveState()
        {
            // TODO:
            return new Dictionary()
            {
                ["joe"] = ""
            };
        }

        public virtual void _LoadState(Dictionary state)
        {
            state[STATE_TRANSFORM] =
        }
    }

    [Tool]
    public class NetworkVolatileRigidBody : NetworkVolatileBody
    {
        protected override VoltBody CreateBody(VoltWorld world, VoltShape[] shapes)
            => world.CreateDynamicBody(GlobalFixedPosition, GlobalFixedRotation, shapes, Layer, Mask);

        public void AddForce(VoltVector2 force)
        {
            Body.AddForce(force);
        }

        public void AddTorque(Fix64 radians)
        {
            Body.AddTorque(radians);
        }

        public void Set(VoltVector2 position, Fix64 radians)
        {
            Body.Set(position, radians);
        }

        public void SetVelocity(VoltVector2 linearVelocity, Fix64 angularVelocity)
        {
            Body.LinearVelocity = linearVelocity;
            Body.AngularVelocity = angularVelocity;
        }

        public void SetForce(VoltVector2 force, Fix64 torque, VoltVector2 biasVelocity, Fix64 biasRotation)
        {
            Body.SetForce(force, torque, biasVelocity, biasRotation);
        }

        public override network
    }
}