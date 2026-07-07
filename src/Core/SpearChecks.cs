namespace TrickSpear;

internal static class SpearChecks
{
    internal static int GetSpearHandMask(Player player)
    {
        if (player.grasps == null)
        {
            return 0;
        }

        var mask = 0;
        for (var i = 0; i < player.grasps.Length; i++)
        {
            if (player.grasps[i]?.grabbed is Spear)
            {
                mask |= 1 << i;
            }
        }

        return mask;
    }

    internal static bool HasSpearInHand(Player player) => GetSpearHandMask(player) != 0;

    internal static bool IsHeldSpear(Spear spear, Player player)
    {
        if (player.grasps == null)
        {
            return false;
        }

        for (var i = 0; i < player.grasps.Length; i++)
        {
            if (player.grasps[i]?.grabbed == spear)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool MaskUnchanged(int currentMask, int startMask) =>
        SpearHandMask.IsUnchanged(currentMask, startMask);

    internal static BodyChunk? GetSpearBodyChunk(Player player)
    {
        if (player.grasps == null)
        {
            return null;
        }

        for (var i = 0; i < player.grasps.Length; i++)
        {
            if (player.grasps[i]?.grabbed is Spear spear)
            {
                return spear.firstChunk;
            }
        }

        return null;
    }
}
