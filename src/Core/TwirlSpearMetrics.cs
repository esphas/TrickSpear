using UnityEngine;

namespace TrickSpear;

internal static class TwirlSpearMetrics
{
    /// <summary>Full spear length from grasp chunk to tip (pose sampling).</summary>
    internal const float TipOffset = 25f;

    /// <summary>Active blade segment: how far back from tip the sweep/hit/trail extends.</summary>
    internal const float BladeShaftReach = 15f;

    /// <summary>How far past the tip the blade extends forward.</summary>
    internal const float BladeForwardReach = 2f;

    /// <summary>Perpendicular tolerance beyond object radius; keep in sync with trail width.</summary>
    internal const float HitPadding = 1f;

    internal static bool IsWithinBladeSweep(
        Vector2 target,
        float targetRadius,
        Vector2 tip,
        Vector2 pointDir)
    {
        var start = tip - pointDir * BladeShaftReach;
        var end = tip + pointDir * BladeForwardReach;
        var dist = TwirlMath.DistancePointToSegment(target, start, end);
        return dist <= targetRadius + HitPadding;
    }
}
