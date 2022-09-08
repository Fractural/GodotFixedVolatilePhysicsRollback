using Godot;
using Godot.Collections;
using GodotRollbackNetcode;
using System;
using Volatile;
using Volatile.GodotEngine;

namespace Game
{
    [Tool]
    public class Bomb : VoltNode2D, INetworkSpawn, INetworkDespawn
    {
        public static Node Spawn(Node parent, VoltVector2 position, Node owner)
        {
            return SyncManager.Global.Spawn(nameof(Bomb), parent, GD.Load<PackedScene>("res://Game/Scenes/Bomb.tscn"), new { position = VoltType.Serialize(position), ownerPath = owner.GetPath().ToString() }.ToGodotDict());
        }

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

        public void _NetworkSpawn(Dictionary data)
        {
            GlobalFixedPosition = VoltType.Deserialize<VoltVector2>((byte[])data["position"]);
            BombOwnerPath = (string)data["ownerPath"];
            explosionTimer.Start();
            animationPlayer.Play("Tick");
        }

        private void OnExplosionTimerTimeout()
        {
            Exploded?.Invoke();
            SyncManager.Global.Spawn("Explosion", GetParent(), explosionPrefab, new { position = VoltType.Serialize(GlobalFixedPosition) }.ToGodotDict());
            SyncManager.Global.Despawn(this);
        }

        public void _NetworkDespawn()
        {
            animationPlayer.Stop(true);
            Exploded = null;
        }
    }
}