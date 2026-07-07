namespace TrickSpear;

internal static class TwirlPoseProfiles
{
    internal static bool CanStart(Player player) =>
        ResolveStartPose(player).HasValue && SpearChecks.HasSpearInHand(player);

    internal static TwirlPose? ResolveAutoTriggerPose(Player player, TwirlAutoTriggerKind kind)
    {
        if (TwirlPoseBlocking.IsUnsupportedBodyMode(player)
            || TwirlPoseBlocking.IsInShortCutTransition(player))
        {
            return null;
        }

        if (!SpearChecks.HasSpearInHand(player))
        {
            return null;
        }

        return kind switch
        {
            TwirlAutoTriggerKind.BellySlide => TwirlPose.BellySlide,
            TwirlAutoTriggerKind.TurnSkid => TwirlPose.Slide,
            TwirlAutoTriggerKind.SlideRoll => IsAirborne(player) ? TwirlPose.Airborne : TwirlPose.BellySlide,
            TwirlAutoTriggerKind.Backflip => TwirlPose.Airborne,
            _ => null,
        };
    }

    internal static TwirlPose? ResolveStartPose(Player player)
    {
        if (TwirlPoseBlocking.DisallowsTwirl(player))
        {
            return null;
        }

        if (player.bodyMode == Player.BodyModeIndex.Crawl)
        {
            return TwirlPose.Crawl;
        }

        if (TwirlPoseMotion.IsBellySlide(player))
        {
            return TwirlPose.BellySlide;
        }

        if (TwirlPoseMotion.IsGroundSlide(player))
        {
            return TwirlPose.Slide;
        }

        if (IsAirborne(player))
        {
            return TwirlPose.Airborne;
        }

        if (player.bodyMode == Player.BodyModeIndex.Stand)
        {
            return TwirlPose.Stand;
        }

        if (player.bodyMode == Player.BodyModeIndex.Default)
        {
            return player.standing ? TwirlPose.Stand : TwirlPose.Airborne;
        }

        return null;
    }

    internal static bool IsAirborne(Player player)
    {
        if (player.standing)
        {
            return false;
        }

        if (player.bodyMode == Player.BodyModeIndex.Crawl)
        {
            return false;
        }

        if (TwirlPoseMotion.IsBellySlide(player))
        {
            return false;
        }

        return player.bodyMode == Player.BodyModeIndex.Default
            || player.bodyMode == Player.BodyModeIndex.Stand;
    }

    internal static int SequenceCount(TwirlPose pose) => TwirlMoves.SequenceCount(pose);
}
