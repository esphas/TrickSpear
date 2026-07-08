using UnityEngine;

namespace TrickSpear;

internal static class SpinObjectInteractLocal
{
    internal const float RockImpulse = 9f;
    internal const float MaxSweepSpeed = 18f;

    internal static void HitByWeapon(PhysicalObject obj, Weapon spear)
    {
        obj.HitByWeapon(spear);
    }

    internal static bool TryDetatchStalk(PhysicalObject obj)
    {
        if (obj is WaterNut nut && nut.stalk != null)
        {
            nut.DetatchStalk();
            return true;
        }

        if (obj is FlareBomb flare && flare.stalk != null)
        {
            flare.DetatchStalk();
            return true;
        }

        if (obj is IHaveAStalk stalked)
        {
            var name = obj.GetType().Name;
            if (name is "LillyPuck" or "DandelionPeach")
            {
                stalked.DetatchStalk();
                return true;
            }
        }

        return false;
    }

    internal static void ApplySweepImpulse(PhysicalObject obj, Vector2 push)
    {
        obj.firstChunk.vel += push;
        obj.firstChunk.vel = Vector2.ClampMagnitude(obj.firstChunk.vel, MaxSweepSpeed);

        if (obj is PuffBall or GraffitiBomb)
        {
            obj.firstChunk.vel *= 0.85f;
        }
    }
}
