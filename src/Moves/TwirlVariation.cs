using UnityEngine;

namespace TrickSpear;

internal static class TwirlVariation
{
    internal const float MinAmplitude = 0.82f;
    internal const float MaxAmplitude = 1.18f;

    internal const float MinSpeedScale = 1f;
    internal const float MaxSpeedScale = 1.25f;
    internal const float ReferenceRunSpeed = 9f;

    internal static void RollSessionParams(Player player, PlayerTwirlState.Data state)
    {
        state.AmplitudeScale = Random.Range(MinAmplitude, MaxAmplitude);
        state.SpeedScale = ComputeSpeedScale(player);
        state.ScaledRaiseFrames = ScaleFrames(PlayerTwirlState.Timing.RaiseFrames, state.SpeedScale, minFrames: 4);
        state.ScaledChainRaiseFrames = ScaleFrames(PlayerTwirlState.Timing.ChainRaiseFrames, state.SpeedScale, minFrames: 3);
        state.ScaledLowerFrames = ScaleFrames(PlayerTwirlState.Timing.LowerFrames, state.SpeedScale, minFrames: 4);
    }

    internal static float ComputeSpeedScale(Player player)
    {
        var speed = player.mainBodyChunk.vel.magnitude;
        return Mathf.Lerp(MinSpeedScale, MaxSpeedScale, Mathf.InverseLerp(0f, ReferenceRunSpeed, speed));
    }

    internal const float ShortPressRotationReduction = 1f;
    internal const float ShortPressMinRotations = 1f;
    internal const int ShortPressMinSpinFrames = 8;

    internal static float ResolveActiveSpinRotations(float baseRotations, bool inHoldSession)
    {
        if (inHoldSession)
        {
            return baseRotations;
        }

        return Mathf.Max(ShortPressMinRotations, baseRotations - ShortPressRotationReduction);
    }

    internal static int ResolveActiveSpinFrames(
        int baseSpinFrames,
        float speedScale,
        float activeRotations,
        float baseRotations)
    {
        var fullFrames = ScaleSpinFrames(baseSpinFrames, speedScale);
        if (Mathf.Approximately(activeRotations, baseRotations))
        {
            return fullFrames;
        }

        var ratio = activeRotations / baseRotations;
        return Mathf.Max(ShortPressMinSpinFrames, Mathf.RoundToInt(fullFrames * ratio));
    }

    internal static int ScaleSpinFrames(int baseFrames, float speedScale) =>
        ScaleFrames(baseFrames, speedScale, minFrames: 14);

    internal static int ScaleFrames(int baseFrames, float speedScale, int minFrames) =>
        Mathf.Max(minFrames, Mathf.RoundToInt(baseFrames / speedScale));
}
