using Godot;
using GodotRollbackNetcode;
using Volatile.GodotEngine;
using Volatile.GodotEngine.Rollback;

namespace Game
{
    [Tool]
    public class BombSpawner : NetworkVolatileArea
    {
        [Export]
        PackedScene bombPrefab;

        protected override void OnBodyEntered(IVolatileBody body)
        {
            base.OnBodyEntered(body);
            TrySpawnBomb(body);
        }

        protected override void OnBodyExited(IVolatileBody body)
        {
            base.OnBodyExited(body);
            TrySpawnBomb(body);
        }

        private void TrySpawnBomb(IVolatileBody body)
        {
            if (body.Body.IsKinematic)
            {
                SyncManager.Global.Spawn(nameof(Bomb), GetParent(), bombPrefab,
                    Bomb.Construct(GlobalFixedPosition, GetPath())
                );
            }
        }
    }
}