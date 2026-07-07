using UnityEngine;

namespace TrickSpear;

internal static class TwirlPhaseSync
{
    internal static void Reset(PlayerTwirlState.Data state)
    {
        state.SpinPreviousPhaseRad = 0f;
    }

    internal static bool TryConsumeHalfRotation(PlayerTwirlState.Data state, out int crossingIndex)
    {
        crossingIndex = -1;

        if (state.CurrentSegment != PlayerTwirlState.Segment.Spin || state.SpinElapsed <= 0)
        {
            return false;
        }

        var phase = TwirlSession.SpinPhaseAngleRadians(state);
        var prevPhase = state.SpinPreviousPhaseRad;
        state.SpinPreviousPhaseRad = phase;

        var prevMark = Mathf.FloorToInt(prevPhase / Mathf.PI);
        var currMark = Mathf.FloorToInt(phase / Mathf.PI);

        if (currMark <= prevMark)
        {
            return false;
        }

        crossingIndex = currMark;
        return true;
    }

    internal static float MoveAccent(PlayerTwirlState.Data state) =>
        1f + state.MoveIndex * 0.07f;

    internal static float MovePitchOffset(PlayerTwirlState.Data state) =>
        state.MoveIndex * 0.06f;
}
