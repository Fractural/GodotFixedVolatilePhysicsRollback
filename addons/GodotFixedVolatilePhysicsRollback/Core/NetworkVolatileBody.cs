using FixMath.NET;
using Fractural.Utils;
using Godot;
using Godot.Collections;
using GodotRollbackNetcode;
using System.Linq;

namespace Volatile.GodotEngine.Rollback
{
    [Tool]
    public abstract class NetworkVolatileBody : NetworkVoltNode2D, INetworkPostProcess, IInterpolateState
    {
        public delegate void BodyCollidedDelegate(VolatileBody body);
        public event BodyCollidedDelegate BodyCollided;

        public VolatileShape[] Shapes { get; set; }
        [Export]
        public bool DoInterpolation { get; set; } = true;
        [Export(PropertyHint.Layers2dPhysics)]
        public int Layer { get; set; } = 1;
        [Export(PropertyHint.Layers2dPhysics)]
        public int Mask { get; set; } = 1;
        public VoltBody Body { get; private set; }

        public override string _GetConfigurationWarning()
        {
            var volatileWorld = this.GetAncestor<NetworkVolatileWorld>(false);
            if (volatileWorld == null)
                return $"This node must be a descendant of a {nameof(NetworkVolatileWorld)}.";

            var shapes = this.GetDescendants<VolatileShape>();
            if (shapes.Count == 0)
                return "This node has no shape, so it can't collide or interact with other objects.\nConsider addinga VolatileShape (VolatilePolygon, VolatileRect, VolatileRect) as a child to define its shape.";
            return "";
        }

        public override void _Ready()
        {
            base._Ready();
#if TOOLS
            if (Engine.EditorHint)
            {
                SetPhysicsProcess(false);
                return;
            }
#endif
            var volatileWorldNode = this.GetAncestor<VolatileWorld>(false);
            if (volatileWorldNode == null)
                return;

            var shapeNodes = this.GetDescendants<VolatileShape>();
            if (shapeNodes.Count == 0)
                return;

            var world = volatileWorldNode.World;
            var shapes = shapeNodes.Select(x => x.PrepareShape(world)).ToArray();

            Body = CreateBody(world, shapes);
            Body.UserData = this;
            Body.BodyCollided += OnBodyCollided;
        }

        protected override void FixedTransformChanged()
        {
            if (!Engine.EditorHint && Body != null)
                Body.Set(GlobalFixedPosition, GlobalFixedRotation);
            base.FixedTransformChanged();
        }

        protected virtual void OnBodyCollided(VoltBody body)
        {
            if (body.UserData is VolatileBody volatileBody)
                BodyCollided?.Invoke(volatileBody);
        }

        protected abstract VoltBody CreateBody(VoltWorld world, VoltShape[] shapes);

        #region Network
        public virtual void _NetworkPostprocess(Dictionary input)
        {
            GlobalFixedPosition = Body.Position;
            GlobalFixedRotation = Body.Angle;
        }

        public virtual void _InterpolateState(Dictionary oldState, Dictionary newState, float weight)
        {
            if (DoInterpolation)
            {
                var oldTransform = oldState.GetVoltDeserialized<VoltTransform2D>(STATE_TRANSFORM);
                var newTransform = newState.GetVoltDeserialized<VoltTransform2D>(STATE_TRANSFORM);
                // Okay to convert float to Fix64 here, because interpolate is client side
                // and not synced to state at all.
                GlobalFixedTransform = oldTransform.InterpolateWith(newTransform, (Fix64)weight);
            }
        }
        #endregion
    }
}