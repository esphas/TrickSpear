using UnityEngine;

namespace TrickSpear;

internal static class TwirlSpearMetrics
{
    internal const float TipOffset = 25f;

    internal const float BladeShaftReach = 15f;

    internal const float HitShaftReach = TipOffset;

    internal const float BladeForwardReach = 4f;

    internal const float HitPadding = 4.5f;

    internal static bool IsWithinBladeSweep(
        Vector2 target,
        float targetRadius,
        Vector2 tip,
        Vector2 pointDir)
    {
        var start = tip - pointDir * HitShaftReach;
        var end = tip + pointDir * BladeForwardReach;
        var dist = TwirlMath.DistancePointToSegment(target, start, end);
        return dist <= targetRadius + HitPadding;
    }

    internal static bool IsWithinBladeSweepVolume(
        Vector2 target,
        float targetRadius,
        Vector2 tip,
        Vector2 dir,
        bool hasPrevSample,
        Vector2 prevTip,
        Vector2 prevDir)
    {
        if (IsWithinBladeSweep(target, targetRadius, tip, dir))
        {
            return true;
        }

        if (!hasPrevSample || prevDir.sqrMagnitude < 0.001f)
        {
            return false;
        }

        return IsWithinBladeSweep(target, targetRadius, prevTip, prevDir);
    }
}
