using UnityEngine;

namespace TrickSpear;

internal static class TwirlSpinFeedback
{
    internal static void OnEnterSpin(Player player, PlayerTwirlState.Data state)
    {
        TwirlPhaseSync.Reset(state);
        TwirlVfx.OnEnterSpin(player, state);
        TwirlSfx.OnEnterSpin(player, state);
    }

    internal static void Update(Player player, PlayerTwirlState.Data state)
    {
        if (player.room == null || !state.IsTwirling)
        {
            StopAll(state);
            return;
        }

        switch (state.CurrentSegment)
        {
            case PlayerTwirlState.Segment.Spin:
                UpdateSpin(player, state);
                break;

            case PlayerTwirlState.Segment.Lower:
                TwirlSfx.StopLoop(state);
                TwirlVfx.UpdateTrail(player, state);
                break;

            default:
                StopAll(state);
                break;
        }
    }

    internal static void StopAll(PlayerTwirlState.Data state)
    {
        TwirlSfx.StopAll(state);
        TwirlVfx.StopAll(state);
    }

    private static void UpdateSpin(Player player, PlayerTwirlState.Data state)
    {
        TwirlSfx.UpdateLoop(player, state);
        TwirlVfx.UpdateTrail(player, state);

        if (!TwirlPhaseSync.TryConsumeHalfRotation(state, out var crossingIndex))
        {
            return;
        }

        TwirlSfx.PlayHalfRevSwish(player, state, crossingIndex);
    }
}
