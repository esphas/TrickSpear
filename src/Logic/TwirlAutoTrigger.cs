using System.Runtime.CompilerServices;

namespace TrickSpear;

internal static class TwirlAutoTrigger
{
    private sealed class LatchData
    {
        public int PrevSlideCounter;
        public Player.AnimationIndex LastAnimation = Player.AnimationIndex.None;
    }

    private static readonly ConditionalWeakTable<Player, LatchData> Latches = new();

    internal static void TryFire(Player player, PlayerTwirlState.Data state)
    {
        if (!TwirlAutoTriggerConfig.Enabled || state.IsTwirling)
        {
            return;
        }

        if (!GameplayGuards.CanAutoStartTwirl(player))
        {
            return;
        }

        var latch = Latches.GetOrCreateValue(player);
        var kind = DetectEdge(player, latch);
        SyncLatch(player, latch);

        if (kind == TwirlAutoTriggerKind.None)
        {
            return;
        }

        if (!TwirlSession.Begin(player, state, chained: false, kind))
        {
            return;
        }

        state.InHoldSession = false;
        TwirlDebug.LogAutoTwirl(player, kind);
        TwirlSfx.PlayStart(player);
    }

    private static TwirlAutoTriggerKind DetectEdge(Player player, LatchData latch)
    {
        var anim = player.animation;

        if (anim != latch.LastAnimation)
        {
            if (anim == Player.AnimationIndex.BellySlide && TwirlAutoTriggerConfig.OnBellySlide)
            {
                return TwirlAutoTriggerKind.BellySlide;
            }

            if (anim == Player.AnimationIndex.Flip && TwirlAutoTriggerConfig.OnBackflip)
            {
                return TwirlAutoTriggerKind.Backflip;
            }

            if (anim == Player.AnimationIndex.Roll && TwirlAutoTriggerConfig.OnSlideRoll)
            {
                return TwirlAutoTriggerKind.SlideRoll;
            }
        }

        if (TwirlAutoTriggerConfig.OnTurnSkid
            && player.bodyMode == Player.BodyModeIndex.Stand
            && player.slideCounter > 0
            && latch.PrevSlideCounter == 0)
        {
            return TwirlAutoTriggerKind.TurnSkid;
        }

        return TwirlAutoTriggerKind.None;
    }

    private static void SyncLatch(Player player, LatchData latch)
    {
        latch.PrevSlideCounter = player.slideCounter;
        latch.LastAnimation = player.animation;
    }
}
