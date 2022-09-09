using Godot;
using Godot.Collections;
using GodotRollbackNetcode;

namespace Volatile.GodotEngine.Rollback
{
    [Tool]
    public class NetworkSimpleForce : NetworkVoltNode2D, INetworkProcess
    {
        #region Force
        private VoltVector2 force;
        public VoltVector2 Force
        {
            get
            {
#if TOOLS
                if (Engine.EditorHint)
                    return VoltType.DeserializeOrDefault<VoltVector2>(_force);
                else
#endif
                    return force;
            }
            set
            {
#if TOOLS
                if (Engine.EditorHint)
                    _force = VoltType.Serialize(value);
                else
#endif
                    force = value;
            }
        }
        [Export(hintString: VoltPropertyHint.VoltVector2)]
        private byte[] _force;
        #endregion

        private IVolatileRigidBody body;

        public override void _Ready()
        {
            base._Ready();
            Force = VoltType.DeserializeOrDefault<VoltVector2>(_force);

            if (Engine.EditorHint)
            {
                SetPhysicsProcess(false);
                return;
            }
            body = GetParent<IVolatileRigidBody>();
        }

        public override string _GetConfigurationWarning()
        {
            if (!(GetParent() is IVolatileRigidBody))
                return $"{nameof(NetworkSimpleForce)} must be a child of a {nameof(IVolatileRigidBody)}!";
            return "";
        }

        public virtual void _NetworkProcess(Dictionary input)
        {
            if (body != null) body.AddForce(Force);
        }
    }
}