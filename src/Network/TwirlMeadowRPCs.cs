using RainMeadow;
using UnityEngine;

namespace TrickSpear;

internal static class TwirlMeadowRPCs
{
    [RPCMethod]
    public static void ApplySpinObjectInteract(
        RPCEvent rpc,
        OnlinePhysicalObject target,
        OnlinePhysicalObject? spearOnline,
        byte kind,
        Vector2 impulse)
    {
        if (target.apo.realizedObject is not PhysicalObject obj)
        {
            return;
        }

        switch ((SpinInteractKind)kind)
        {
            case SpinInteractKind.HitByWeapon:
                if (spearOnline?.apo.realizedObject is Weapon spear)
                {
                    SpinObjectInteractLocal.HitByWeapon(obj, spear);
                }

                break;

            case SpinInteractKind.DetatchStalk:
                SpinObjectInteractLocal.TryDetatchStalk(obj);
                break;

            case SpinInteractKind.Sweep:
                SpinObjectInteractLocal.ApplySweepImpulse(obj, impulse);
                break;
        }
    }

    [RPCMethod]
    public static void RetractLizardTongue(RPCEvent rpc, OnlineCreature target)
    {
        if (target.realizedCreature is Lizard { tongue: not null } lizard)
        {
            lizard.tongue.Retract();
        }
    }

    [RPCMethod]
    public static void ApplyCreatureRepel(RPCEvent rpc, OnlineCreature target, Vector2 velDelta, int stunFrames)
    {
        if (target.realizedCreature is not Creature crit || crit.dead)
        {
            return;
        }

        crit.mainBodyChunk.vel += velDelta;
        crit.Stun(stunFrames);
    }
}
