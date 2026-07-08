namespace TrickSpear;

internal static class ParryHooks
{
    internal static void Apply()
    {
        On.Creature.Violence += CreatureViolence;
    }

    private static void CreatureViolence(
        On.Creature.orig_Violence orig,
        Creature self,
        BodyChunk source,
        UnityEngine.Vector2? directionAndMomentum,
        BodyChunk hitChunk,
        PhysicalObject.Appendage.Pos hitAppendage,
        Creature.DamageType type,
        float damage,
        float stunBonus)
    {
        if (self is Player player
            && !player.isNPC
            && TwirlNetworkGuard.AllowCombatForPlayer(player))
        {
            var state = PlayerTwirlState.Get(player);
            if (SpinParry.TryBlockViolence(player, state, source, type, damage))
            {
                return;
            }
        }

        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }
}
