using RainMeadow;
using System;
using UnityEngine;

namespace TrickSpear;

internal static class TwirlMeadowCombat
{
    internal static bool UseOnlineCombatSync =>
        TwirlNetworkGuard.IsOnlineSession && TwirlNetworkGuard.AllowCombatPhysics;

    internal static void DeflectThrownWeapon(Weapon weapon, Vector2 deflectVel, Player parryingPlayer, Room? room)
    {
        ApplyWeaponDeflectLocal(weapon, deflectVel);
        TwirlCombatFeedback.PlayParry(room, weapon.firstChunk.pos);
        SyncWeaponDeflect(weapon, parryingPlayer);
    }

    internal static void RetractLizardTongue(Lizard lizard) =>
        DispatchCreatureAuthority(
            lizard.abstractCreature,
            () => lizard.tongue?.Retract(),
            onlineCreature => onlineCreature.owner.InvokeRPC(
                TwirlMeadowRPCs.RetractLizardTongue,
                onlineCreature));

    internal static void RepelCreature(Creature crit, Vector2 velDelta, int stunFrames) =>
        DispatchCreatureAuthority(
            crit.abstractCreature,
            () =>
            {
                crit.mainBodyChunk.vel += velDelta;
                crit.Stun(stunFrames);
            },
            onlineCreature => onlineCreature.owner.InvokeRPC(
                TwirlMeadowRPCs.ApplyCreatureRepel,
                onlineCreature,
                velDelta,
                stunFrames));

    internal static bool ApplyObjectInteraction(
        PhysicalObject obj,
        Spear spear,
        SpinInteractKind kind,
        Vector2 impulse,
        Func<bool> tryLocalApply)
    {
        if (TwirlMeadowCombat.UseOnlineCombatSync && !spear.abstractPhysicalObject.IsLocal())
        {
            return false;
        }

        var applied = false;
        DispatchObjectAuthority(
            obj.abstractPhysicalObject,
            () => applied = tryLocalApply(),
            targetOnline =>
            {
                OnlinePhysicalObject? spearOnline =
                    spear.abstractPhysicalObject.GetOnlineObject() as OnlinePhysicalObject;
                targetOnline.owner.InvokeRPC(
                    TwirlMeadowRPCs.ApplySpinObjectInteract,
                    targetOnline,
                    spearOnline!,
                    (byte)kind,
                    impulse);
                applied = true;
            });

        return applied;
    }

    private static void DispatchCreatureAuthority(
        AbstractCreature creature,
        Action localApply,
        Action<OnlineCreature> remoteApply)
    {
        if (!UseOnlineCombatSync || creature.IsLocal())
        {
            localApply();
            return;
        }

        if (creature.GetOnlineCreature() is OnlineCreature onlineCreature)
        {
            remoteApply(onlineCreature);
        }
    }

    private static void DispatchObjectAuthority(
        AbstractPhysicalObject apo,
        Action localApply,
        Action<OnlinePhysicalObject> remoteApply)
    {
        if (!UseOnlineCombatSync || apo.IsLocal())
        {
            localApply();
            return;
        }

        if (apo.GetOnlineObject() is OnlinePhysicalObject targetOnline)
        {
            remoteApply(targetOnline);
            return;
        }

        localApply();
    }

    private static void SyncWeaponDeflect(Weapon weapon, Player parryingPlayer)
    {
        if (!UseOnlineCombatSync || !parryingPlayer.abstractCreature.IsLocal())
        {
            return;
        }

        if (weapon.abstractPhysicalObject.GetOnlineObject() is not OnlinePhysicalObject onlineWeapon
            || onlineWeapon.isMine
            || TwirlMeadowBindings.WeaponCreatureDeflect == null)
        {
            return;
        }

        var state = CreateWeaponState(onlineWeapon);
        if (state == null)
        {
            return;
        }

        var rpcEvent = onlineWeapon.owner.InvokeRPC(
            TwirlMeadowBindings.WeaponCreatureDeflect,
            onlineWeapon,
            state,
            false,
            false);
        TwirlMeadowBindings.LockEntity(onlineWeapon, "parry", rpcEvent);
        BroadcastWeaponDeflect(onlineWeapon, state);
    }

    internal static void ApplyWeaponDeflectLocal(Weapon weapon, Vector2 deflectVel)
    {
        weapon.ChangeMode(Weapon.Mode.Free);
        weapon.firstChunk.vel = deflectVel;
        weapon.SetRandomSpin();
    }

    private static RealizedWeaponState? CreateWeaponState(OnlinePhysicalObject onlineWeapon)
    {
        return onlineWeapon.apo.realizedObject switch
        {
            Spear => new RealizedSpearState(onlineWeapon),
            Weapon => new RealizedWeaponState(onlineWeapon),
            _ => null,
        };
    }

    private static void BroadcastWeaponDeflect(OnlinePhysicalObject onlineWeapon, RealizedWeaponState state)
    {
        var participants = onlineWeapon.roomSession?.participants;
        if (participants == null || TwirlMeadowBindings.WeaponCreatureDeflect == null)
        {
            return;
        }

        foreach (var participant in participants)
        {
            if (participant is not null && participant != onlineWeapon.owner && !participant.isMe)
            {
                participant.InvokeRPC(
                    TwirlMeadowBindings.WeaponCreatureDeflect,
                    onlineWeapon,
                    state,
                    false,
                    false);
            }
        }
    }
}
