using Godot;

namespace Volatile.GodotEngine.Rollback
{
    [Tool]
    public class NetworkVolatileKinematicBody : NetworkVolatileBody, IVolatileKinematicBody
    {
        protected override VoltBody CreateBody(VoltWorld world, VoltShape[] shapes)
            => world.CreateKinematicBody(GlobalFixedPosition, GlobalFixedRotation, shapes, Layer, Mask);

        public VoltKinematicCollisionResult MoveAndCollide(VoltVector2 linearVelocity)
        {
            return Body.MoveAndCollide(linearVelocity);
        }

        public VoltVector2 MoveAndSlide(VoltVector2 linearVelocity, int maxSlides = 4)
        {
            return Body.MoveAndSlide(linearVelocity, maxSlides);
        }
    }
}