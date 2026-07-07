namespace TrickSpear;

internal static class TwirlPoseBlocking
{
    internal static bool DisallowsTwirl(Player player) =>
        IsUnsupportedBodyMode(player)
        || IsMacroMove(player)
        || IsInShortCutTransition(player);

    internal static bool DisallowsDuringTwirl(Player player, TwirlPose activePose)
    {
        if (IsUnsupportedBodyMode(player) || IsInShortCutTransition(player))
        {
            return true;
        }

        if (activePose == TwirlPose.Airborne && !player.standing)
        {
            return false;
        }

        if (activePose == TwirlPose.Slide && TwirlPoseMotion.IsGroundSlide(player))
        {
            return false;
        }

        if (activePose == TwirlPose.BellySlide && TwirlPoseMotion.IsBellySlide(player))
        {
            return false;
        }

        return IsMacroMove(player);
    }

    internal static bool IsUnsupportedBodyMode(Player player)
    {
        if (player.bodyMode == Player.BodyModeIndex.CorridorClimb)
        {
            return true;
        }

        if (player.bodyMode == Player.BodyModeIndex.ClimbIntoShortCut)
        {
            return true;
        }

        if (player.bodyMode == Player.BodyModeIndex.WallClimb)
        {
            return true;
        }

        if (player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam)
        {
            return true;
        }

        if (player.bodyMode == Player.BodyModeIndex.Swimming)
        {
            return true;
        }

        if (player.bodyMode == Player.BodyModeIndex.ZeroG)
        {
            return true;
        }

        if (player.bodyMode == Player.BodyModeIndex.Stunned)
        {
            return true;
        }

        if (player.bodyMode == Player.BodyModeIndex.Dead)
        {
            return true;
        }

        return false;
    }

    internal static bool IsMacroMove(Player player)
    {
        var anim = player.animation;
        if (!player.standing
            && (anim == Player.AnimationIndex.Flip
                || anim == Player.AnimationIndex.Roll
                || anim == Player.AnimationIndex.RocketJump))
        {
            return false;
        }

        if (anim == Player.AnimationIndex.BellySlide)
        {
            return false;
        }

        if (anim == Player.AnimationIndex.Roll)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.Flip)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.RocketJump)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.CorridorTurn)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.HangFromBeam)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.ClimbOnBeam)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.GetUpOnBeam)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.StandOnBeam)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.BeamTip)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.HangUnderVerticalBeam)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.SurfaceSwim)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.DeepSwim)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.ZeroGSwim)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.ZeroGPoleGrab)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.VineGrab)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.AntlerClimb)
        {
            return true;
        }

        if (anim == Player.AnimationIndex.GrapplingSwing)
        {
            return true;
        }

        return false;
    }

    internal static bool IsInShortCutTransition(Player player) =>
        player.enteringShortCut.HasValue || player.inShortcut;
}
