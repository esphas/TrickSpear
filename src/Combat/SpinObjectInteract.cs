using System.Reflection;
using UnityEngine;

namespace TrickSpear;

internal static class SpinObjectInteract
{
    private const float RockImpulse = 9f;
    private const float MaxSweepMass = 0.22f;
    private const float MaxSweepSpeed = 18f;

    internal static void Update(Player player, PlayerTwirlState.Data state)
    {
        if (!TwirlCombatConfig.SpinSmallObjectInteract || !TwirlCombatWindow.IsInSpinSegment(state))
        {
            return;
        }

        if (!TwirlSpearPose.TryGet(player, state, out var tip, out var dir) || player.room == null)
        {
            return;
        }

        var spear = GetHeldSpear(player);
        if (spear == null)
        {
            return;
        }

        foreach (var objList in player.room.physicalObjects)
        {
            foreach (var obj in objList)
            {
                TryInteract(obj, spear, tip, dir);
            }
        }
    }

    private static void TryInteract(PhysicalObject obj, Spear spear, Vector2 tip, Vector2 dir)
    {
        if (IsExcluded(obj, spear))
        {
            return;
        }

        if (!TwirlSpearMetrics.IsWithinBladeSweep(
                obj.firstChunk.pos,
                obj.firstChunk.rad,
                tip,
                dir))
        {
            return;
        }

        if (TryWeaponInteract(obj, spear))
        {
            return;
        }

        if (TryDetachStalk(obj))
        {
            return;
        }

        TrySweep(obj, tip, dir);
    }

    private static bool IsExcluded(PhysicalObject obj, Spear spear)
    {
        if (obj == null || obj.slatedForDeletetion || obj == spear)
        {
            return true;
        }

        if (obj is Creature or DataPearl or ScavengerBomb or SeedCob or SporePlant)
        {
            return true;
        }

        if (obj is Spear && obj.GetType().Name == "ExplosiveSpear")
        {
            return true;
        }

        var typeName = obj.abstractPhysicalObject.type.ToString();
        return typeName is "BlinkingFlower"
            or "DeadSeedCob"
            or "SingularityBomb"
            or "PebblesPearl"
            or "Oracle";
    }

    private static bool TryWeaponInteract(PhysicalObject obj, Spear spear)
    {
        if (obj is PuffBall or GraffitiBomb)
        {
            return false;
        }

        if (obj is DangleFruit or NeedleEgg or Mushroom or BubbleGrass or KarmaFlower
            or FlyLure or FirecrackerPlant or SlimeMold or JellyFish)
        {
            if (!IsStillAttached(obj))
            {
                return false;
            }

            obj.HitByWeapon(spear);
            return true;
        }

        if (obj is PlayerCarryableItem && IsHangingConsumableType(obj))
        {
            if (!IsStillAttached(obj))
            {
                return false;
            }

            obj.HitByWeapon(spear);
            return true;
        }

        return false;
    }

    private static bool IsHangingConsumableType(PhysicalObject obj)
    {
        var name = obj.GetType().Name;
        return name is "GlowWeed" or "GooieDuck";
    }

    private static bool TryDetachStalk(PhysicalObject obj)
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

        if (obj is IHaveAStalk stalked && IsStillAttached(obj))
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

    private static bool TrySweep(PhysicalObject obj, Vector2 tip, Vector2 dir)
    {
        if (!CanSweep(obj))
        {
            return false;
        }

        var impulseScale = obj is Rock ? 1f : 0.78f;
        var push = dir * (RockImpulse * impulseScale) + new Vector2(0f, 3f * impulseScale);
        obj.firstChunk.vel += push;
        obj.firstChunk.vel = Vector2.ClampMagnitude(obj.firstChunk.vel, MaxSweepSpeed);

        if (obj is PuffBall or GraffitiBomb)
        {
            obj.firstChunk.vel *= 0.85f;
        }

        return true;
    }

    private static bool CanSweep(PhysicalObject obj)
    {
        if (obj is Rock or EggBugEgg or PuffBall or GraffitiBomb)
        {
            return true;
        }

        if (obj is WaterNut nut && nut.stalk == null)
        {
            return true;
        }

        if (obj is FlareBomb flare && flare.stalk == null)
        {
            return true;
        }

        if (obj is Weapon weapon && weapon.mode != Weapon.Mode.Thrown && obj.TotalMass <= MaxSweepMass)
        {
            return weapon.GetType().Name != "ExplosiveSpear";
        }

        if (obj is PlayerCarryableItem && obj.TotalMass <= MaxSweepMass && obj is not DataPearl)
        {
            return true;
        }

        return false;
    }

    private static bool IsStillAttached(PhysicalObject obj)
    {
        if (TryGetFieldValue(obj, "growPos", out var growPos) && growPos is Vector2?)
        {
            return ((Vector2?)growPos).HasValue;
        }

        if (TryGetFieldValue(obj, "myStalk", out var myStalk) && myStalk != null)
        {
            return TryGetIntField(myStalk, "releaseCounter") is 0;
        }

        if (TryGetFieldValue(obj, "stalk", out var stalk) && stalk != null)
        {
            if (obj is WaterNut or FlareBomb)
            {
                return true;
            }

            var release = TryGetIntField(stalk, "releaseCounter");
            return release is null or 0;
        }

        return obj is FlyLure or FirecrackerPlant or SlimeMold or JellyFish
            || obj.GetType().Name == "GooieDuck";
    }

    private static bool TryGetFieldValue(object target, string fieldName, out object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        if (field == null)
        {
            value = null;
            return false;
        }

        value = field.GetValue(target);
        return true;
    }

    private static int? TryGetIntField(object target, string fieldName)
    {
        if (!TryGetFieldValue(target, fieldName, out var value) || value == null)
        {
            return null;
        }

        return value is int i ? i : null;
    }

    private static Spear? GetHeldSpear(Player player)
    {
        if (player.grasps == null)
        {
            return null;
        }

        for (var i = 0; i < player.grasps.Length; i++)
        {
            if (player.grasps[i]?.grabbed is Spear spear)
            {
                return spear;
            }
        }

        return null;
    }
}
