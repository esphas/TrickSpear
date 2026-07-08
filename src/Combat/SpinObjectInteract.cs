using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TrickSpear;

internal static class SpinObjectInteract
{
    private const float MaxSweepMass = 0.22f;
    private const BindingFlags InstanceFieldFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Update(Player player, PlayerTwirlState.Data state)
    {
        if (!TwirlNetworkGuard.AllowCombatPhysics)
        {
            return;
        }

        if (!TwirlCombatConfig.SpinSmallObjectInteract || !TwirlCombatWindow.IsInSpinSegment(state))
        {
            return;
        }

        if (!TwirlSpearPose.TryGet(player, state, out var tip, out var dir) || player.room == null)
        {
            return;
        }

        var spear = SpearChecks.GetHeldSpear(player);
        if (spear == null)
        {
            return;
        }

        state.SpinInteractedObjects ??= new HashSet<PhysicalObject>();
        var candidates = state.SpinInteractCandidates;
        candidates.Clear();

        CollectCandidates(player.room, state, spear, tip, dir, candidates);

        for (var i = 0; i < candidates.Count; i++)
        {
            ApplyInteraction(state, candidates[i], spear, dir);
        }

        state.SpinLastBladeTip = tip;
        state.SpinLastBladeDir = dir;
        state.SpinHasLastBladeSample = true;
    }

    /// <summary>
    /// Scan all collision layers, then apply. Two phases avoid mutating room lists during iteration.
    /// </summary>
    private static void CollectCandidates(
        Room room,
        PlayerTwirlState.Data state,
        Spear spear,
        Vector2 tip,
        Vector2 dir,
        List<PhysicalObject> candidates)
    {
        var interacted = state.SpinInteractedObjects!;

        foreach (var objList in room.physicalObjects)
        {
            for (var i = 0; i < objList.Count; i++)
            {
                var obj = objList[i];
                if (IsExcluded(obj, spear) || interacted.Contains(obj) || !IsPotentialTarget(obj))
                {
                    continue;
                }

                if (!TwirlSpearMetrics.IsWithinBladeSweepVolume(
                        obj.firstChunk.pos,
                        obj.firstChunk.rad,
                        tip,
                        dir,
                        state.SpinHasLastBladeSample,
                        state.SpinLastBladeTip,
                        state.SpinLastBladeDir))
                {
                    continue;
                }

                candidates.Add(obj);
            }
        }
    }

    private static void ApplyInteraction(
        PlayerTwirlState.Data state,
        PhysicalObject obj,
        Spear spear,
        Vector2 dir)
    {
        if (TryWeaponInteract(obj, spear)
            || TryDetachStalk(obj, spear)
            || TrySweep(obj, spear, dir))
        {
            state.SpinInteractedObjects!.Add(obj);
        }
    }

    private static bool IsPotentialTarget(PhysicalObject obj)
    {
        if (obj is Rock or EggBugEgg or PuffBall or GraffitiBomb)
        {
            return true;
        }

        if (obj is WaterNut or FlareBomb)
        {
            return true;
        }

        if (IsHangingWeaponTarget(obj))
        {
            return true;
        }

        if (obj is PlayerCarryableItem && IsHangingConsumableType(obj))
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

        if (!IsHangingWeaponTarget(obj)
            && !(obj is PlayerCarryableItem && IsHangingConsumableType(obj)))
        {
            return false;
        }

        if (!IsStillAttached(obj))
        {
            return false;
        }

        return DispatchInteraction(
            obj,
            spear,
            SpinInteractKind.HitByWeapon,
            Vector2.zero,
            () =>
            {
                SpinObjectInteractLocal.HitByWeapon(obj, spear);
                return true;
            });
    }

    private static bool TryDetachStalk(PhysicalObject obj, Spear spear)
    {
        if (!CanDetachStalk(obj))
        {
            return false;
        }

        return DispatchInteraction(
            obj,
            spear,
            SpinInteractKind.DetatchStalk,
            Vector2.zero,
            () => SpinObjectInteractLocal.TryDetatchStalk(obj));
    }

    private static bool TrySweep(PhysicalObject obj, Spear spear, Vector2 dir)
    {
        if (!CanSweep(obj))
        {
            return false;
        }

        var impulseScale = obj is Rock ? 1f : 0.78f;
        var push = dir * (SpinObjectInteractLocal.RockImpulse * impulseScale)
            + new Vector2(0f, 3f * impulseScale);

        return DispatchInteraction(
            obj,
            spear,
            SpinInteractKind.Sweep,
            push,
            () =>
            {
                SpinObjectInteractLocal.ApplySweepImpulse(obj, push);
                return true;
            });
    }

    private static bool DispatchInteraction(
        PhysicalObject obj,
        Spear spear,
        SpinInteractKind kind,
        Vector2 impulse,
        System.Func<bool> localApply)
    {
        if (!TwirlMeadowCombat.UseOnlineCombatSync)
        {
            return localApply();
        }

        return TwirlMeadowCombat.ApplyObjectInteraction(obj, spear, kind, impulse, localApply);
    }

    private static bool IsHangingWeaponTarget(PhysicalObject obj) =>
        obj is DangleFruit or NeedleEgg or Mushroom or BubbleGrass or KarmaFlower
            or FlyLure or FirecrackerPlant or SlimeMold or JellyFish;

    private static bool IsHangingConsumableType(PhysicalObject obj)
    {
        var name = obj.GetType().Name;
        return name is "GlowWeed" or "GooieDuck";
    }

    private static bool CanDetachStalk(PhysicalObject obj)
    {
        if (obj is WaterNut nut && nut.stalk != null)
        {
            return true;
        }

        if (obj is FlareBomb flare && flare.stalk != null)
        {
            return true;
        }

        if (obj is IHaveAStalk && IsStillAttached(obj))
        {
            return obj.GetType().Name is "LillyPuck" or "DandelionPeach";
        }

        return false;
    }

    private static bool CanSweep(PhysicalObject obj)
    {
        if (obj.grabbedBy.Count > 0)
        {
            return false;
        }

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
        if (obj is DangleFruit fruit)
        {
            return fruit.stalk != null && fruit.stalk.releaseCounter == 0;
        }

        if (obj is NeedleEgg egg)
        {
            return egg.stalk != null && egg.stalk.releaseCounter == 0;
        }

        if (obj is BubbleGrass or FlyLure or FirecrackerPlant)
        {
            return TryReadGrowPos(obj, out var hasGrowPos) && hasGrowPos;
        }

        if (obj is KarmaFlower karmaFlower)
        {
            return karmaFlower.growPos.HasValue;
        }

        if (obj is Mushroom mushroom)
        {
            return mushroom.growPos.HasValue;
        }

        if (IsHangingConsumableType(obj))
        {
            return IsHangingConsumableAttached(obj);
        }

        if (obj is SlimeMold or JellyFish)
        {
            return obj.grabbedBy.Count == 0 && obj.firstChunk.ContactPoint != default;
        }

        return false;
    }

    private static bool IsHangingConsumableAttached(PhysicalObject obj)
    {
        if (obj.abstractPhysicalObject is AbstractConsumable consumable && consumable.isConsumed)
        {
            return false;
        }

        if (TryGetFieldValue(obj, "stalk", out var stalk) && stalk != null)
        {
            return TryGetIntField(stalk, "releaseCounter") is null or 0;
        }

        if (TryGetFieldValue(obj, "myStalk", out var myStalk) && myStalk != null)
        {
            return TryGetIntField(myStalk, "releaseCounter") is null or 0;
        }

        return false;
    }

    private static bool TryReadGrowPos(object target, out bool hasGrowPos)
    {
        hasGrowPos = false;
        if (!TryGetFieldValue(target, "growPos", out var raw) || raw == null)
        {
            return false;
        }

        if (raw is Vector2)
        {
            hasGrowPos = true;
            return true;
        }

        return false;
    }

    private static bool TryGetFieldValue(object target, string fieldName, out object? value)
    {
        var field = target.GetType().GetField(fieldName, InstanceFieldFlags);
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
}
