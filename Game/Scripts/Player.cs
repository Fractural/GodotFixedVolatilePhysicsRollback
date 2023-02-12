using FixMath.NET;
using Godot;
using Godot.Collections;
using GodotRollbackNetcode;
using System;
using Volatile;
using Volatile.GodotEngine;
using Volatile.GodotEngine.Rollback;

namespace Game
{
    [Tool]
    public class Player : NetworkVolatileKinematicBody, IGetLocalInput, INetworkProcess, INetworkSerializable, IPredictRemoteInput, IInterpolateState
    {
        public const string STATE_TELEPORTING = "teleporting";
        public const string STATE_SPEED = "speed";

        [Export]
        PackedScene bombPrefab;
        [Export]
        PackedScene explosionPrefab;
        NetworkRandomNumberGenerator rng;

        [Export]
        public string inputPrefix = "player1_";

        public Fix64 Speed { get; set; }
        [Export(hintString: VoltPropertyHint.Fix64)]
        byte[] _speed;

        bool teleporting = false;

        public override void _Ready()
        {
            base._Ready();
            if (Engine.EditorHint) return;

            SyncManager.Global.SceneSpawned += OnSyncManagerSceneSpawned;
            SyncManager.Global.SceneDespawned += OnSyncManagerSceneDespawned;

            Speed = VoltType.Deserialize<Fix64>(_speed);

            rng = this.GetNodeAsWrapper<NetworkRandomNumberGenerator>("NetworkRandomNumberGenerator");
        }

        private void OnSyncManagerSceneDespawned(string name, Node node)
        {
            if (node is Bomb bomb && bomb.IsOwnedBy(this))
            {
                bomb.Exploded -= OnBombExploded;
            }
        }

        private void OnSyncManagerSceneSpawned(string name, Node spawnedNode, PackedScene scene, Dictionary data)
        {
            if (spawnedNode is Bomb bomb && bomb.IsOwnedBy(this))
            {
                bomb.Exploded += OnBombExploded;
            }
        }

        private void OnBombExploded()
        {
            SyncManager.Global.Spawn(nameof(Explosion), GetParent(), explosionPrefab,
                Explosion.Construct(GlobalFixedPosition)
            );
        }


        private Vector2 GetSimulatedVector(string left, string right, string up, string down)
        {
            Vector2 result = Vector2.Zero;
            if (Input.IsActionPressed(left)) result.x -= 1;
            if (Input.IsActionPressed(right)) result.x += 1;
            if (Input.IsActionPressed(up)) result.y -= 1;
            if (Input.IsActionPressed(down)) result.y += 1;
            return result;
        }

        public Dictionary _GetLocalInput()
        {
            var inputVector = GetSimulatedVector(inputPrefix + "left", inputPrefix + "right", inputPrefix + "up", inputPrefix + "down");

            var input = new Dictionary();
            if (inputVector != Vector2.Zero)
                input["input_vector"] = inputVector;
            if (Input.IsActionJustPressed(inputPrefix + "bomb"))
                input["drop_bomb"] = true;
            if (Input.IsActionJustPressed(inputPrefix + "teleport"))
                input["teleport"] = true;

            return input;
        }

        public virtual void _NetworkProcess(Dictionary input)
        {
            var inputVector = input.Get("input_vector", Vector2.Zero).ToVoltVector2();
            if (inputVector != VoltVector2.Zero)
            {
                if (Speed < (Fix64)20)
                    Speed += (Fix64)2;
                MoveAndSlide(inputVector * Speed);
            }
            else
            {
                Speed = Fix64.Zero;
            }
        }

        public override void _NetworkPostprocess(Dictionary input)
        {
            // Sync transform with body position
            base._NetworkPostprocess(input);

            if (input.Get("drop_bomb", false))
                SyncManager.Global.Spawn(nameof(Bomb), GetParent(), bombPrefab,
                    Bomb.Construct(GlobalFixedPosition, GetPath())
                );
            if (input.Get("teleport", false))
            {
                var position = new VoltVector2(
                    Fix64.Abs(Fix64.From(rng.Randi() % 1024)),
                    Fix64.Abs(Fix64.From(rng.Randi() % 600))
                );
                GlobalFixedPosition = position;
                teleporting = true;
            }
            else
                teleporting = false;
        }

        public override Dictionary _SaveState()
        {
            var state = base._SaveState();
            state.AddVoltSerialized(STATE_SPEED, Speed);
            state[STATE_TELEPORTING] = teleporting;

            return state;
        }

        public override void _LoadState(Dictionary state)
        {
            base._LoadState(state);
            Speed = state.GetVoltDeserialized<Fix64>(STATE_SPEED);
            teleporting = state.Get<bool>(STATE_TELEPORTING);
        }

        public Dictionary _PredictRemoteInput(Dictionary previousInput, int ticksSinceRealInput)
        {
            var input = previousInput.Duplicate();
            input.Remove("drop_bomb");
            if (ticksSinceRealInput > 2)
                input.Remove("input_vector");
            return input;
        }

        public override void _InterpolateState(Dictionary oldState, Dictionary newState, float weight)
        {
            // Don't interpolate body if we're teleporting
            if (oldState.Get("teleporting", false) || newState.Get("teleporting", false))
                return;

            // Interpolate the volatile body
            base._InterpolateState(oldState, newState, weight);
        }

        public void Seed(NetworkRandomNumberGenerator johnny)
        {
            rng.Seed = johnny.Randi();
        }
    }
}