namespace TrickSpear;

internal static class TwirlContext
{
    internal static bool TryGetSpearOwner(Spear spear, out Player player)
    {
        player = null!;
        if (spear.grabbedBy == null || spear.grabbedBy.Count == 0)
        {
            return false;
        }

        if (spear.grabbedBy[0].grabber is not Player owner || owner.isNPC)
        {
            return false;
        }

        player = owner;
        return true;
    }

    internal static bool IsGraphicsReady(Player player, PlayerTwirlState.Data state) =>
        state.IsTwirling
        && SpearHandMask.IsUnchanged(SpearChecks.GetSpearHandMask(player), state.StartSpearHandMask);
}
