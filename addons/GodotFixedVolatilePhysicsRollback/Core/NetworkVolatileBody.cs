using FixMath.NET;
using Fractural.Utils;
using Godot;
using Godot.Collections;
using GodotRollbackNetcode;
using System.Linq;

namespace Volatile.GodotEngine.Rollback
{
    [Tool]
    public abstract class NetworkVolatileBody : NetworkVoltNode2D, INetworkPostProcess, IInterpolateState, IVolatileBody
    {
        VoltNode2D IVolatileBody.Node => this;
        public event BodyCollidedDelegate BodyCollided;

        public VolatileShape[] Shapes { get; set; }
        [Export]
        public bool DoInterpolation { get; set; } = true;
        [Export(PropertyHint.Layers2dPhysics)]
        public int Layer { get; set; } = 1;
        [Export(PropertyHint.Layers2dPhysics)]
        public int Mask { get; set; } = 1;
        public VoltBody Body { get; private set; }

        /// <summary>
        /// Prevents lerping from setting the body position + rotation, which
        /// would then affect the network state of this body.
        /// </summary>
        protected bool stopFixedToBodySync = false;

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
            if (!Engine.EditorHint && Body != null && !stopFixedToBodySync)
                Body.Set(GlobalFixedPosition, GlobalFixedRotation);
            base.FixedTransformChanged();
        }

        protected virtual void OnBodyCollided(VoltBody body)
        {
            if (body.UserData is IVolatileBody volatileBody)
                BodyCollided?.Invoke(volatileBody);
        }

        protected abstract VoltBody CreateBody(VoltWorld world, VoltShape[] shapes);

        #region Network
        public virtual void _NetworkPostprocess(Dictionary input)
        {
            stopFixedToBodySync = true;
            GlobalFixedPosition = Body.Position;
            GlobalFixedRotation = Body.Angle;
            stopFixedToBodySync = false;
        }

        public virtual void _InterpolateState(Dictionary oldState, Dictionary newState, float weight)
        {
            if (DoInterpolation)
            {
                var oldBodyPosition = oldState.GetVoltDeserialized<VoltVector2>(STATE_BODY_POSITION);
                var oldBodyRotation = oldState.GetVoltDeserialized<Fix64>(STATE_BODY_ROTATION);
                var oldTransform = new VoltTransform2D(oldBodyRotation, oldBodyPosition);

                var newBodyPosition = newState.GetVoltDeserialized<VoltVector2>(STATE_BODY_POSITION);
                var newBodyRotation = newState.GetVoltDeserialized<Fix64>(STATE_BODY_ROTATION);
                var newTransform = new VoltTransform2D(newBodyRotation, newBodyPosition);

                stopFixedToBodySync = true;
                // Okay to convert float to Fix64 here, because interpolate is client side
                // and not synced to state at all.
                GlobalFixedTransform = oldTransform.InterpolateWith(newTransform, (Fix64)weight);
                stopFixedToBodySync = false;
            }
        }

        public const string STATE_BODY_POSITION = "body_position";
        public const string STATE_BODY_ROTATION = "body_rotation";

        public override Dictionary _SaveState()
        {
            // Don't use VoltNode2D's save state
            var state = new Dictionary();
            state.AddVoltSerialized(STATE_BODY_POSITION, Body.Position);
            state.AddVoltSerialized(STATE_BODY_ROTATION, Body.Angle);
            return state;
        }

        public override void _LoadState(Dictionary state)
        {
            // Don't use VoltNode2D's load state
            //base._LoadState(state);
            var bodyPosition = state.GetVoltDeserialized<VoltVector2>(STATE_BODY_POSITION);
            var bodyRotation = state.GetVoltDeserialized<Fix64>(STATE_BODY_ROTATION);
            Body.Set(bodyPosition, bodyRotation);
            stopFixedToBodySync = true;
            GlobalFixedPosition = bodyPosition;
            GlobalFixedRotation = bodyRotation;
            stopFixedToBodySync = false;
        }

        #endregion
    }
}