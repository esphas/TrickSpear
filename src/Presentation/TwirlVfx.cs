using UnityEngine;

namespace TrickSpear;

internal static class TwirlVfx
{
    internal const float LowerTrailIntensity = 0.58f;
    internal const float LowerTrailEndIntensity = 0.36f;

    internal static void OnEnterSpin(Player player, PlayerTwirlState.Data state)
    {
        ReleaseTrail(state);

        if (player.room == null || !TwirlSpearPose.TryGet(player, state, out var tip, out var dir))
        {
            return;
        }

        var trail = CreateTrail(state);
        player.room.AddObject(trail);
        trail.AddSample(tip, dir);
        state.SpinTrail = trail;
    }

    internal static void UpdateTrail(Player player, PlayerTwirlState.Data state)
    {
        if (player.room == null || state.SpinTrail == null || !state.SpinTrail.AcceptingSamples)
        {
            return;
        }

        state.SpinTrail.SetIntensity(ResolveTrailIntensity(state));

        if (!TwirlSpearPose.TryGet(player, state, out var tip, out var dir))
        {
            return;
        }

        if (!player.room.ViewedByAnyCamera(tip, 100f))
        {
            return;
        }

        state.SpinTrail.AddSample(tip, dir);
    }

    internal static void StopAll(PlayerTwirlState.Data state) => ReleaseTrail(state);

    private static void ReleaseTrail(PlayerTwirlState.Data state)
    {
        if (state.SpinTrail == null)
        {
            return;
        }

        state.SpinTrail.BeginFadeOut();
        state.SpinTrail = null;
    }

    private static TwirlRotationTrail CreateTrail(PlayerTwirlState.Data state)
    {
        var width = Mathf.Lerp(2.4f, 3.6f, state.AmplitudeScale) * TwirlPhaseSync.MoveAccent(state);
        var alpha = Mathf.Lerp(0.44f, 0.64f, state.AmplitudeScale);
        var color = new Color(1f, 0.99f, 0.94f, alpha);
        return new TwirlRotationTrail(color, width);
    }

    private static float ResolveTrailIntensity(PlayerTwirlState.Data state)
    {
        if (state.CurrentSegment == PlayerTwirlState.Segment.Spin)
        {
            return 1f;
        }

        if (state.CurrentSegment == PlayerTwirlState.Segment.Lower)
        {
            var progress = Mathf.Clamp01(
                TwirlMath.SegmentProgress(state.SegmentTimer, state.ScaledLowerFrames));
            return Mathf.Lerp(LowerTrailIntensity, LowerTrailEndIntensity, progress);
        }

        return 1f;
    }
}
