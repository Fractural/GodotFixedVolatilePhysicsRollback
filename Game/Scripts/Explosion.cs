using Godot;
using Godot.Collections;
using GodotRollbackNetcode;
using System;
using Volatile;
using Volatile.GodotEngine;

namespace Game
{
    public class Explosion : VoltNode2D, INetworkSpawn, INetworkDespawn
    {
        NetworkTimer despawnTimer;
        NetworkAnimationPlayer animationPlayer;
        [Export]
        AudioStream explosionSound;

        public void _NetworkDespawn()
        {
            animationPlayer.Stop(true);
        }

        public void _NetworkSpawn(Dictionary data)
        {
            GlobalFixedPosition = VoltType.Deserialize<VoltVector2>((byte[])data["position"]);
            despawnTimer.Start();
            animationPlayer.Play("Explode");
            SyncManager.Global.PlaySound($"{GetPath()}:Explosion", explosionSound);
        }

        public override void _Ready()
        {
            despawnTimer = this.GetNodeAsWrapper<NetworkTimer>("DespawnTimer");
            despawnTimer.Timeout += OnDespawnTimerTimeout;

            animationPlayer = this.GetNodeAsWrapper<NetworkAnimationPlayer>("NetworkAnimationPlayer");
        }

        private void OnDespawnTimerTimeout()
        {
            SyncManager.Global.Despawn(this);
        }
    }
}
