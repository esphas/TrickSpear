using RWCustom;
using UnityEngine;

namespace TrickSpear;

internal static class SpinParry
{
    private const float WeaponScanReach = 52f;
    private const float TongueScanReach = 38f;
    private const float CreatureScanReach = 40f;
    private const float DeflectSpeed = 25f;

    internal static void Update(Player player, PlayerTwirlState.Data state)
    {
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
        if (!TwirlCombatConfig.SpinParryWindow || !TwirlCombatWindow.IsInParryWindow(state))
        {
            return false;
        }

        if (!IsParryableViolence(player, source, type, damage))
        {
            return false;
        }

        if (TwirlSpearPose.TryGet(player, state, out var tip, out _))
        {
            PlayParryFeedback(player.room, tip);
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

    private static void DeflectThrownWeapons(Player player, Vector2 tip, Vector2 dir)
    {
        var room = player.room;
        if (room == null)
        {
            return;
        }

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

                if (Vector2.Distance(tip, weapon.firstChunk.pos) > WeaponScanReach)
                {
                    continue;
                }

                var toWeapon = weapon.firstChunk.pos - tip;
                if (toWeapon.sqrMagnitude > 0.01f && Vector2.Dot(toWeapon.normalized, dir) < 0.2f)
                {
                    continue;
                }

                weapon.ChangeMode(Weapon.Mode.Free);
                weapon.firstChunk.vel = dir * DeflectSpeed + new Vector2(0f, 6f);
                weapon.SetRandomSpin();
                PlayParryFeedback(room, weapon.firstChunk.pos);
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
                lizard.tongue.Retract();
                PlayParryFeedback(room, tip);
                continue;
            }

            if (lizard.tongue.state != LizardTongue.State.LashingOut)
            {
                continue;
            }

            if (Vector2.Distance(tip, lizard.tongue.pos) > TongueScanReach)
            {
                continue;
            }

            var toTongue = lizard.tongue.pos - tip;
            if (toTongue.sqrMagnitude > 0.01f && Vector2.Dot(toTongue.normalized, dir) < 0.15f)
            {
                continue;
            }

            lizard.tongue.Retract();
            PlayParryFeedback(room, lizard.tongue.pos);
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
            if (Vector2.Distance(tip, chunk.pos) > CreatureScanReach)
            {
                continue;
            }

            var away = chunk.pos - tip;
            if (away.sqrMagnitude < 0.01f)
            {
                away = -dir;
            }

            away.Normalize();
            if (Vector2.Dot(away, dir) < 0.25f)
            {
                continue;
            }

            chunk.vel += away * 8f + dir * 4f;
            crit.Stun(40);
            PlayParryFeedback(room, chunk.pos);
        }
    }

    private static void PlayParryFeedback(Room? room, Vector2 pos)
    {
        if (room == null)
        {
            return;
        }

        room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, pos);
        room.AddObject(new ExplosionSpikes(
            room,
            pos,
            8,
            30f,
            7f,
            7f,
            60f,
            new Color(1f, 1f, 1f, 0.8f)));
    }
}
