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
    public class Bomb : NetworkVoltNode2D, INetworkSpawn, INetworkDespawn
    {
        public static Dictionary Construct(VoltVector2 position, string ownerPath)
        {
            return new Dictionary()
            {
                [SPAWN_POSITION] = VoltType.Serialize(position),
                [SPAWN_OWNER_PATH] = ownerPath
            };
        }

        public const string SPAWN_OWNER_PATH = "owner_path";
        public event Action Exploded;
        public NodePath BombOwnerPath { get; private set; }

        [Export]
        PackedScene explosionPrefab;
        NetworkTimer explosionTimer;
        NetworkAnimationPlayer animationPlayer;

        public override void _Ready()
        {
            base._Ready();
            if (Engine.EditorHint) return;
            explosionTimer = this.GetNodeAsWrapper<NetworkTimer>("ExplosionTimer");
            explosionTimer.Connect("timeout", this, nameof(OnExplosionTimerTimeout));

            animationPlayer = this.GetNodeAsWrapper<NetworkAnimationPlayer>("NetworkAnimationPlayer");
        }

        public bool IsOwnedBy(Node node)
        {
            if (!IsInstanceValid(node)) return false;
            return node.GetPath().ToString() == BombOwnerPath;
        }

        public override void _NetworkSpawn(Dictionary data)
        {
            base._NetworkSpawn(data);
            BombOwnerPath = data.Get<string>(SPAWN_OWNER_PATH);
            explosionTimer.Start();
            animationPlayer.Play("Tick");
        }

        private void OnExplosionTimerTimeout()
        {
            Exploded?.Invoke();

            SyncManager.Global.Spawn(nameof(Explosion), GetParent(), explosionPrefab,
                Explosion.Construct(GlobalFixedPosition)
            );
            SyncManager.Global.Despawn(this);
        }

        public void _NetworkDespawn()
        {
            animationPlayer.Stop(true);
            Exploded = null;
        }
    }
}