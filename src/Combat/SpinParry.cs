using RWCustom;
using UnityEngine;

namespace TrickSpear;

internal static class SpinParry
{
    private const float WeaponScanReach = 52f;
    private const float TongueScanReach = 38f;
    private const float CreatureScanReach = 40f;
    private const float DeflectSpeed = 25f;
    private const float DeflectLift = 6f;

    internal static void Update(Player player, PlayerTwirlState.Data state)
    {
        if (!TwirlNetworkGuard.AllowCombatPhysics)
        {
            return;
        }

        if (!TwirlCombatConfig.SpinParryWindow || !TwirlCombatWindow.IsInParryWindow(state))
        {
            return;
        }

        if (!TwirlSpearPose.TryGet(player, state, out var tip, out var dir) || player.room == null)
        {
            return;
        }

        DeflectThrownWeapons(player, tip, dir);
        DeflectLizardTongues(player, tip, dir);
        RepelSmallStabbers(player, tip, dir);
    }

    internal static bool TryBlockViolence(
        Player player,
        PlayerTwirlState.Data state,
        BodyChunk? source,
        Creature.DamageType type,
        float damage)
    {
        if (!TwirlNetworkGuard.AllowCombatPhysics
            || !TwirlCombatConfig.SpinParryWindow
            || !TwirlCombatWindow.IsInParryWindow(state))
        {
            return false;
        }

        if (!IsParryableViolence(player, source, type, damage))
        {
            return false;
        }

        if (!TwirlSpearPose.TryGet(player, state, out var tip, out var dir))
        {
            return true;
        }

        if (source?.owner is Weapon weapon
            && weapon.mode == Weapon.Mode.Thrown
            && weapon.thrownBy != player)
        {
            TwirlMeadowCombat.DeflectThrownWeapon(
                weapon,
                ComputeThrownDeflectVelocity(dir),
                player,
                player.room);
        }
        else
        {
            TwirlCombatFeedback.PlayParry(player.room, tip);
        }

        return true;
    }

    internal static bool IsParryableViolence(
        Player player,
        BodyChunk? source,
        Creature.DamageType type,
        float damage)
    {
        if (source?.owner == null)
        {
            return false;
        }

        if (source.owner is Weapon weapon &&
            weapon.mode == Weapon.Mode.Thrown &&
            weapon.thrownBy != player)
        {
            return true;
        }

        if (source.owner is Creature crit && crit != player)
        {
            if (crit is Lizard lizard &&
                lizard.tongue != null &&
                (lizard.tongue.state == LizardTongue.State.LashingOut ||
                 lizard.tongue.state == LizardTongue.State.Attatched))
            {
                return true;
            }

            if (crit is NeedleWorm or TubeWorm)
            {
                return type == Creature.DamageType.Stab || damage <= 1.6f;
            }

            if (crit.Template.smallCreature && type == Creature.DamageType.Stab)
            {
                return true;
            }
        }

        return false;
    }

    private static Vector2 ComputeThrownDeflectVelocity(Vector2 dir) =>
        dir * DeflectSpeed + new Vector2(0f, DeflectLift);

    private static void DeflectThrownWeapons(Player player, Vector2 tip, Vector2 dir)
    {
        var room = player.room;
        if (room == null)
        {
            return;
        }

        var deflectVel = ComputeThrownDeflectVelocity(dir);

        foreach (var objList in room.physicalObjects)
        {
            foreach (var obj in objList)
            {
                if (obj is not Weapon weapon ||
                    weapon.mode != Weapon.Mode.Thrown ||
                    weapon.thrownBy == player)
                {
                    continue;
                }

                if (!IsWithinReach(tip, weapon.firstChunk.pos, WeaponScanReach, dir, weapon.firstChunk.pos - tip, 0.2f))
                {
                    continue;
                }

                TwirlMeadowCombat.DeflectThrownWeapon(weapon, deflectVel, player, room);
            }
        }
    }

    private static void DeflectLizardTongues(Player player, Vector2 tip, Vector2 dir)
    {
        var room = player.room;
        if (room == null)
        {
            return;
        }

        foreach (var crit in room.physicalObjects[1])
        {
            if (crit is not Lizard lizard || lizard.tongue == null || !lizard.tongue.Out)
            {
                continue;
            }

            if (lizard.tongue.state != LizardTongue.State.LashingOut &&
                lizard.tongue.state != LizardTongue.State.Attatched)
            {
                continue;
            }

            if (lizard.tongue.attached?.owner == player &&
                lizard.tongue.state == LizardTongue.State.Attatched)
            {
                TwirlMeadowCombat.RetractLizardTongue(lizard);
                TwirlCombatFeedback.PlayParry(room, tip);
                continue;
            }

            if (lizard.tongue.state != LizardTongue.State.LashingOut)
            {
                continue;
            }

            if (!IsWithinReach(tip, lizard.tongue.pos, TongueScanReach, dir, lizard.tongue.pos - tip, 0.15f))
            {
                continue;
            }

            TwirlMeadowCombat.RetractLizardTongue(lizard);
            TwirlCombatFeedback.PlayParry(room, lizard.tongue.pos);
        }
    }

    private static void RepelSmallStabbers(Player player, Vector2 tip, Vector2 dir)
    {
        var room = player.room;
        if (room == null)
        {
            return;
        }

        foreach (var critObj in room.physicalObjects[1])
        {
            if (critObj is not Creature crit || crit == player || crit.dead)
            {
                continue;
            }

            if (crit is not NeedleWorm and not TubeWorm && !crit.Template.smallCreature)
            {
                continue;
            }

            var chunk = crit.mainBodyChunk;
            var away = chunk.pos - tip;
            if (away.sqrMagnitude < 0.01f)
            {
                away = -dir;
            }

            if (!IsWithinReach(tip, chunk.pos, CreatureScanReach, dir, away, 0.25f))
            {
                continue;
            }

            away.Normalize();
            TwirlMeadowCombat.RepelCreature(crit, away * 8f + dir * 4f, 40);
            TwirlCombatFeedback.PlayParry(room, chunk.pos);
        }
    }

    private static bool IsWithinReach(
        Vector2 tip,
        Vector2 targetPos,
        float maxDistance,
        Vector2 dir,
        Vector2 toTarget,
        float minDot)
    {
        if (Vector2.Distance(tip, targetPos) > maxDistance)
        {
            return false;
        }

        if (toTarget.sqrMagnitude <= 0.01f)
        {
            return true;
        }

        return Vector2.Dot(toTarget.normalized, dir) >= minDot;
    }
}
