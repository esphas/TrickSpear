namespace TrickSpear;

internal static class TwirlInterrupts
{
    internal static bool ShouldHardAbort(Player player, PlayerTwirlState.Data state) =>
        GetHardAbortReason(player, state) != null;

    internal static string? GetHardAbortReason(Player player, PlayerTwirlState.Data state)
    {
        if (player.slatedForDeletetion || player.dead)
        {
            return "dead";
        }

        if (!player.Consious)
        {
            return "unconscious";
        }

        if (!SpearChecks.HasSpearInHand(player))
        {
            return "no-spear";
        }

        var currentMask = SpearChecks.GetSpearHandMask(player);
        if (!SpearChecks.MaskUnchanged(currentMask, state.StartSpearHandMask))
        {
            return $"spear-mask {currentMask}!={state.StartSpearHandMask}";
        }

        if (IsGrabbed(player))
        {
            return "grabbed";
        }

        return null;
    }

    internal static void ApplyDuringTwirl(Player player, PlayerTwirlState.Data state)
    {
        if (player.Stunned || TwirlPoseBlocking.DisallowsDuringTwirl(player, state.ActivePose))
        {
            if (!AllowMacroDuringAutoTwirl(player, state))
            {
                TwirlPoseTransition.RequestGracefulLower(state);
            }
        }
    }

    internal static bool AllowMacroDuringAutoTwirl(Player player, PlayerTwirlState.Data state)
    {
        if (state.AutoTriggerKind == TwirlAutoTriggerKind.None)
        {
            return false;
        }

        return state.AutoTriggerKind switch
        {
            TwirlAutoTriggerKind.BellySlide => TwirlPoseMotion.IsBellySlide(player),
            TwirlAutoTriggerKind.TurnSkid =>
                player.bodyMode == Player.BodyModeIndex.Stand && player.slideCounter > 0,
            TwirlAutoTriggerKind.SlideRoll =>
                player.animation == Player.AnimationIndex.Roll
                || TwirlPoseMotion.IsBellySlide(player),
            TwirlAutoTriggerKind.Backflip =>
                player.animation == Player.AnimationIndex.Flip,
            _ => false,
        };
    }

    internal static bool IsGrabbed(Player player) =>
        player.grabbedBy != null && player.grabbedBy.Count > 0
        || player.dangerGrasp != null;
}
