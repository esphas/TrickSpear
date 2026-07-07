using UnityEngine;

namespace TrickSpear;

internal static class TwirlSpearPose
{
    internal static bool TryGet(
        Player player,
        PlayerTwirlState.Data state,
        out Vector2 tip,
        out Vector2 dir,
        out Vector2 shaftRoot)
    {
        tip = default;
        dir = default;
        shaftRoot = default;

        var chunk = SpearChecks.GetSpearBodyChunk(player);
        if (chunk == null)
        {
            return false;
        }

        var handleDir = ResolveHandleDirection(player, state);
        if (handleDir.sqrMagnitude < 0.001f)
        {
            return false;
        }

        handleDir.Normalize();
        dir = -handleDir;
        shaftRoot = chunk.pos;
        tip = shaftRoot + dir * TwirlSpearMetrics.TipOffset;
        return true;
    }

    internal static bool TryGet(
        Player player,
        PlayerTwirlState.Data state,
        out Vector2 tip,
        out Vector2 dir) =>
        TryGet(player, state, out tip, out dir, out _);

    private static Vector2 ResolveHandleDirection(Player player, PlayerTwirlState.Data state)
    {
        if (player.grasps != null)
        {
            for (var i = 0; i < player.grasps.Length; i++)
            {
                if (player.grasps[i]?.grabbed is Spear spear && spear.rotation.sqrMagnitude > 0.001f)
                {
                    return spear.rotation;
                }
            }
        }

        return TwirlSession.SpearOrientation(state);
    }
}
