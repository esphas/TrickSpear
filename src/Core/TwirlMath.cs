using UnityEngine;

namespace TrickSpear;

internal static class TwirlMath
{
    internal static float SmoothStep(float t) => t * t * (3f - 2f * t);

    internal static float EaseInQuad(float t) => t * t;

    internal static float EaseOutQuad(float t)
    {
        var inv = 1f - t;
        return 1f - inv * inv;
    }

    internal static float SegmentProgress(int remaining, int total) =>
        total <= 0 ? 1f : 1f - remaining / (float)total;

    internal static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f)
        {
            return Vector2.Distance(point, a);
        }

        var t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lenSq);
        return Vector2.Distance(point, a + ab * t);
    }
}
