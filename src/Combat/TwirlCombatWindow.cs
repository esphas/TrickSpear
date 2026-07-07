namespace TrickSpear;

internal static class TwirlCombatWindow
{
    internal static bool IsInSpinSegment(PlayerTwirlState.Data state) =>
        state.IsTwirling && state.CurrentSegment == PlayerTwirlState.Segment.Spin;

    internal static bool IsInParryWindow(PlayerTwirlState.Data state) =>
        IsInSpinSegment(state) &&
        state.SpinElapsed >= 0 &&
        state.SpinElapsed <= TwirlCombatConfig.ParryWindowFrames;
}
