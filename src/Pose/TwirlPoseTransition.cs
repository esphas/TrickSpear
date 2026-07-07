using UnityEngine;

namespace TrickSpear;

internal static class TwirlPoseTransition
{
    internal const int BlendFrames = 6;

    internal static void Update(Player player, PlayerTwirlState.Data state)
    {
        if (!state.IsTwirling)
        {
            return;
        }

        var resolved = TwirlPoseProfiles.ResolveStartPose(player);
        var flipChanged = player.flipDirection != state.StartFlipDirection;

        if (resolved == state.ActivePose && !flipChanged)
        {
            return;
        }

        if (resolved == null)
        {
            RequestGracefulLower(state);
            return;
        }

        if (player.graphicsModule is not PlayerGraphics graphics)
        {
            return;
        }

        BeginBlend(player, state, graphics, resolved.Value);
    }

    internal static void Tick(PlayerTwirlState.Data state)
    {
        if (state.PoseTransitionRemaining > 0)
        {
            state.PoseTransitionRemaining--;
        }
    }

    internal static float BlendProgress(PlayerTwirlState.Data state)
    {
        if (state.PoseTransitionRemaining <= 0)
        {
            return 1f;
        }

        var t = TwirlMath.SmoothStep(1f - state.PoseTransitionRemaining / (float)BlendFrames);
        return t;
    }

    internal static Vector2 ApplyBlend(PlayerTwirlState.Data state, int handIndex, Vector2 target)
    {
        if (state.PoseTransitionRemaining <= 0)
        {
            return target;
        }

        return Vector2.Lerp(state.TransitionFromRelative[handIndex], target, BlendProgress(state));
    }

    private static void BeginBlend(
        Player player,
        PlayerTwirlState.Data state,
        PlayerGraphics graphics,
        TwirlPose newPose)
    {
        for (var handIndex = 0; handIndex < SpearHandMask.HandCount; handIndex++)
        {
            if (!SpearHandMask.Includes(state.StartSpearHandMask, handIndex))
            {
                continue;
            }

            state.TransitionFromRelative[handIndex] = graphics.hands[handIndex].relativeHuntPos;
        }

        state.ActivePose = newPose;
        state.StartFlipDirection = player.flipDirection;
        state.MoveIndex %= TwirlPoseProfiles.SequenceCount(newPose);
        state.PoseTransitionRemaining = BlendFrames;

        if (state.CurrentSegment != PlayerTwirlState.Segment.Spin)
        {
            state.StartHoldAngleDeg = TwirlMoves.ResolveStartHoldAngleDeg(player, newPose);
        }

        TwirlVisuals.RefreshAnchors(graphics, state);
    }

    internal static void RequestGracefulLower(PlayerTwirlState.Data state)
    {
        if (state.CurrentSegment == PlayerTwirlState.Segment.Lower)
        {
            return;
        }

        state.CurrentSegment = PlayerTwirlState.Segment.Lower;
        state.SegmentTimer = state.ScaledLowerFrames;
        state.PoseTransitionRemaining = 0;
    }
}
