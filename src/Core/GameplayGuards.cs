namespace TrickSpear;

internal static class GameplayGuards
{
    internal static bool IsInGameplaySession(Player player)
    {
        if (player == null || player.slatedForDeletetion || player.dead || player.isNPC)
        {
            return false;
        }

        if (player.room == null || player.room.game == null || player.room.game.session == null)
        {
            return false;
        }

        return true;
    }

    internal static bool CanStartTwirl(Player player)
    {
        if (!IsInGameplaySession(player))
        {
            return false;
        }

        if (!player.Consious || player.Stunned)
        {
            return false;
        }

        if (TwirlInterrupts.IsGrabbed(player))
        {
            return false;
        }

        return TwirlPoseProfiles.CanStart(player);
    }

    internal static bool CanAutoStartTwirl(Player player)
    {
        if (!IsInGameplaySession(player))
        {
            return false;
        }

        if (!player.Consious || player.Stunned)
        {
            return false;
        }

        if (TwirlInterrupts.IsGrabbed(player))
        {
            return false;
        }

        return SpearChecks.HasSpearInHand(player);
    }

    internal static string DescribeStartBlock(Player player)
    {
        if (!IsInGameplaySession(player))
        {
            return "no-session";
        }

        if (!player.Consious)
        {
            return "unconscious";
        }

        if (player.Stunned)
        {
            return "stunned";
        }

        if (TwirlInterrupts.IsGrabbed(player))
        {
            return "grabbed";
        }

        if (!SpearChecks.HasSpearInHand(player))
        {
            return "no-spear";
        }

        if (TwirlPoseBlocking.DisallowsTwirl(player))
        {
            return "pose-blocked";
        }

        if (!TwirlPoseProfiles.ResolveStartPose(player).HasValue)
        {
            return "pose-unresolved";
        }

        return "unknown";
    }
}
