namespace TrickSpear;

internal static class TwirlPoseMotion
{
    internal static bool IsGroundSlide(Player player) =>
        player.bodyMode == Player.BodyModeIndex.Stand && player.slideCounter > 0;

    internal static bool IsBellySlide(Player player) =>
        player.animation == Player.AnimationIndex.BellySlide;
}
