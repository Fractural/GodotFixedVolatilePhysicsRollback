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
            ProcessSelf = false;
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
            SyncManager.Global.Spawn("Explosion", GetParent(), explosionPrefab, new { position = VoltType.Serialize(GlobalFixedPosition) }.ToGodotDict());
        }

        public Dictionary _GetLocalInput()
        {
            var inputVector = Input.GetVector(inputPrefix + "left", inputPrefix + "right", inputPrefix + "up", inputPrefix + "down");

            var input = new Dictionary();
            if (inputVector != Vector2.Zero)
                input["input_vector"] = inputVector;
            if (Input.IsActionJustPressed(inputPrefix + "bomb"))
                input["drop_bomb"] = true;
            if (Input.IsActionJustPressed(inputPrefix + "teleport"))
                input["teleport"] = true;

            return input;
        }

        public void _NetworkProcess(Dictionary input)
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

            // Update body position
            GlobalFixedPosition = Body.Position;
            GlobalFixedRotation = Body.Angle;

            if (input.Get("drop_bomb", false))
                Bomb.Spawn(GetParent(), GlobalFixedPosition, this);
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

        public Dictionary _SaveState()
        {
            var state = new Dictionary();

            state["transform"] = VoltType.Serialize(FixedTransform);
            state["speed"] = VoltType.Serialize(Speed);
            state["teleporting"] = teleporting;

            return state;
        }

        public void _LoadState(Dictionary state)
        {
            FixedTransform = VoltType.Deserialize<VoltTransform2D>((byte[])state["transform"]);
            Speed = VoltType.Deserialize<Fix64>((byte[])state["speed"]);
            teleporting = (bool)state["teleporting"];
        }

        public Dictionary _PredictRemoteInput(Dictionary previousInput, int ticksSinceRealInput)
        {
            var input = previousInput.Duplicate();
            input.Remove("drop_bomb");
            if (ticksSinceRealInput > 2)
                input.Remove("input_vector");
            return input;
        }

        public void _InterpolateState(Dictionary oldState, Dictionary newState, float weight)
        {
            if (oldState.Get("teleporting", false) || newState.Get("teleporting", false))
                return;
            var oldTransform = VoltType.Deserialize<VoltTransform2D>((byte[])oldState["transform"]);
            var newTransform = VoltType.Deserialize<VoltTransform2D>((byte[])newState["transform"]);

            GlobalFixedTransform = oldTransform.InterpolateWith(newTransform, (Fix64)weight);
        }

        public void Seed(NetworkRandomNumberGenerator johnny)
        {
            rng.Seed = johnny.Randi();
        }
    }
}