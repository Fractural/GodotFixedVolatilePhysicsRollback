using Godot;
using Godot.Collections;
using GodotRollbackNetcode;
using System;
using Volatile;
using Volatile.GodotEngine;
using Volatile.GodotEngine.Rollback;

namespace Game
{
    public class Explosion : NetworkVoltNode2D, INetworkSpawn, INetworkDespawn
    {
        public static Dictionary Construct(VoltVector2 position)
        {
            return new Dictionary()
            {
                [SPAWN_POSITION] = VoltType.Serialize(position)
            };
        }

        NetworkTimer despawnTimer;
        NetworkAnimationPlayer animationPlayer;
        [Export]
        AudioStream explosionSound;

        public void _NetworkDespawn()
        {
            animationPlayer.Stop(true);
        }

        public override void _NetworkSpawn(Dictionary data)
        {
            base._NetworkSpawn(data);
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
