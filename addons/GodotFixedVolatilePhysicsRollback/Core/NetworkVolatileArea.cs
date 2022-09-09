using Godot;
using Godot.Collections;
using GodotRollbackNetcode;
using System.Collections.Generic;
using System.Linq;

namespace Volatile.GodotEngine.Rollback
{
    [Tool]
    public class NetworkVolatileArea : NetworkVolatileBody, IVolatileArea
    {
        public event BodyEnteredDelegate BodyEntered;
        public event BodyExitedDelegate BodyExited;

        /// <summary>
        /// Should the area keep track of what bodies are inside of it at 
        /// all times in order to fire entered and exited events?
        /// </summary>
        [Export]
        public bool AutoQuery { get; set; } = false;
        [Export]
        public bool AutoQueryDynamicBodies { get; set; } = true;

        /// <summary>
        /// All the bodies that this area was previous colliding with.
        /// </summary>
        protected HashSet<IVolatileBody> PrevCollidingWith { get; set; }
        /// <summary>
        /// All the bodies that this area is currently colliding with.
        /// </summary>
        protected HashSet<IVolatileBody> CurrCollidingWith { get; set; }

        protected override VoltBody CreateBody(VoltWorld world, VoltShape[] shapes)
            => world.CreateTriggerBody(GlobalFixedPosition, GlobalFixedRotation, shapes, Layer, Mask);

        public VoltBodyCollisionResult QueryCollisions(bool collideDynamic = false)
            => QueryCollisions(collideDynamic, VoltCollisionFilters.DefaultWorldCollisionFilter);
        public VoltBodyCollisionResult QueryCollisions(bool collideDynamic, VoltCollisionFilter filter)
        {
            return Body.QueryTriggerCollisions(collideDynamic, filter);
        }

        public override void _Ready()
        {
            base._Ready();
            if (Engine.EditorHint) return;
            if (AutoQuery)
            {
                PrevCollidingWith = new HashSet<IVolatileBody>();
                CurrCollidingWith = new HashSet<IVolatileBody>();
            }
        }

        protected override void OnBodyCollided(VoltBody body)
        {
            base.OnBodyCollided(body);
            if (AutoQuery && body.UserData is IVolatileBody volatileBody)
                CurrCollidingWith.Add(volatileBody);
        }

        protected virtual void OnBodyEntered(IVolatileBody body)
        {
            BodyEntered?.Invoke(body);
        }

        protected virtual void OnBodyExited(IVolatileBody body)
        {
            BodyExited?.Invoke(body);
        }

        #region Network
        public const string STATE_PREV_COLLIDING_WITH = "prev_colliding_with";
        public const string STATE_CURR_COLLIDING_WITH = "curr_colliding_with";

        public override Dictionary _SaveState()
        {
            var dict = base._SaveState();
            if (AutoQuery)
            {
                dict[STATE_PREV_COLLIDING_WITH] = PrevCollidingWith.Select(x => x.Node.GetPath().ToString()).ToArray();
                dict[STATE_CURR_COLLIDING_WITH] = CurrCollidingWith.Select(x => x.Node.GetPath().ToString()).ToArray();
            }
            return dict;
        }

        public override void _LoadState(Dictionary state)
        {
            base._LoadState(state);
            if (AutoQuery)
            {
                var prevCollidingWithPaths = state.Get<string[]>(STATE_PREV_COLLIDING_WITH);
                PrevCollidingWith.Clear();
                foreach (var path in prevCollidingWithPaths)
                    PrevCollidingWith.Add(GetNode<IVolatileBody>(path));

                var currCollidingWithPaths = state.Get<string[]>(STATE_CURR_COLLIDING_WITH);
                CurrCollidingWith.Clear();
                foreach (var path in currCollidingWithPaths)
                    CurrCollidingWith.Add(GetNode<IVolatileBody>(path));
            }
        }

        public override void _NetworkPostprocess(Dictionary input)
        {
            base._NetworkPostprocess(input);
            if (AutoQuery)
            {
                foreach (var oldCollidingBody in PrevCollidingWith)
                    if (!CurrCollidingWith.Contains(oldCollidingBody))
                        OnBodyExited(oldCollidingBody);

                foreach (var newCollidingBody in CurrCollidingWith)
                    if (!PrevCollidingWith.Contains(newCollidingBody))
                        OnBodyEntered(newCollidingBody);

                // Swap PreviousColliding with CurrColliding, and then clear CurrColliding
                // This effectively transfers CurrColliding into PreviousColliding, and 
                // clears CurrColliding for future use.
                var temp = PrevCollidingWith;
                PrevCollidingWith = CurrCollidingWith;
                CurrCollidingWith = temp;
                CurrCollidingWith.Clear();
            }
        }
        #endregion
    }
}