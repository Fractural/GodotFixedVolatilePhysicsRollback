using FixMath.NET;
using Godot;
using Godot.Collections;

namespace Volatile.GodotEngine.Rollback
{
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

        #region Network
        public const string STATE_LINEAR_VELOCITY = "linear_velocity";
        public const string STATE_ANGULAR_VELOCITY = "angular_velocity";

        public override Dictionary _SaveState()
        {
            var dict = base._SaveState();
            dict.AddVoltSerialized(STATE_LINEAR_VELOCITY, Body.LinearVelocity);
            dict.AddVoltSerialized(STATE_ANGULAR_VELOCITY, Body.AngularVelocity);
            return dict;
        }

        public override void _LoadState(Dictionary state)
        {
            base._LoadState(state);
            Body.LinearVelocity = state.GetVoltDeserialized<VoltVector2>(STATE_LINEAR_VELOCITY);
            Body.AngularVelocity = state.GetVoltDeserialized<Fix64>(STATE_ANGULAR_VELOCITY);
        }
        #endregion
    }
}