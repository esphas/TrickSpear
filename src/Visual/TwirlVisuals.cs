using RWCustom;
using UnityEngine;

namespace TrickSpear;

internal static class TwirlVisuals
{
    internal static void Apply(PlayerGraphics graphics, Player player, PlayerTwirlState.Data state)
    {
        if (!state.HandAnchorsCaptured)
        {
            RefreshAnchors(graphics, state);
        }

        if (!state.LookCaptured)
        {
            state.LockedLookDirection = graphics.lookDirection;
            state.LookCaptured = true;
        }

        var move = TwirlSession.ActiveMove(state);
        var lift = TwirlSession.GetLiftBlend(state);
        var lookHandPos = player.bodyChunks[1].pos;
        var hasLookHand = false;

        for (var handIndex = 0; handIndex < SpearHandMask.HandCount; handIndex++)
        {
            if (!SpearHandMask.Includes(state.StartSpearHandMask, handIndex))
            {
                continue;
            }

            if (player.grasps[handIndex]?.grabbed is not Spear)
            {
                continue;
            }

            var hand = graphics.hands[handIndex];
            var target = Vector2.Lerp(
                state.RestRelativeHuntPos[handIndex],
                state.RaisedRelativeHuntPos[handIndex],
                lift);

            target += ComputeWristOffset(state, move);
            var relative = TwirlPoseTransition.ApplyBlend(state, handIndex, target);

            hand.reachingForObject = false;
            hand.mode = Limb.Mode.HuntRelativePosition;
            hand.relativeHuntPos = relative;
            hand.huntSpeed = ResolveHandHuntSpeed(state.ActivePose);
            hand.quickness = ResolveHandQuickness(state.ActivePose);

            lookHandPos = RelativeHuntToWorld(hand, relative);
            hasLookHand = true;
        }

        ApplyLookDirection(graphics, state, hasLookHand ? lookHandPos : (Vector2?)null);
        ClearSpearDir(graphics);
    }

    internal static void RefreshAnchors(PlayerGraphics graphics, PlayerTwirlState.Data state)
    {
        var move = TwirlSession.ActiveMove(state);

        for (var handIndex = 0; handIndex < SpearHandMask.HandCount; handIndex++)
        {
            if (!SpearHandMask.Includes(state.StartSpearHandMask, handIndex))
            {
                continue;
            }

            Vector2 rest;
            Vector2 raised;

            if (UsesFixedAnchors(state.ActivePose))
            {
                rest = ResolveFixedGrasp(state.ActivePose, handIndex, state.StartFlipDirection);
                raised = TwirlMoves.ComputeRaisedRelative(
                    state.ActivePose,
                    rest,
                    handIndex,
                    move,
                    state.AmplitudeScale,
                    state.StartFlipDirection);

                if (state.ActivePose == TwirlPose.Crawl && !state.CrawlAnchorLogged)
                {
                    TwirlDebug.LogCrawlAnchors(handIndex, rest, raised);
                }
            }
            else
            {
                var hand = graphics.hands[handIndex];
                rest = hand.relativeHuntPos;
                if (hand.mode != Limb.Mode.HuntRelativePosition)
                {
                    rest = TwirlMoves.DefaultGraspRelative(state.ActivePose, handIndex);
                }

                raised = TwirlMoves.ComputeRaisedRelative(
                    state.ActivePose,
                    rest,
                    handIndex,
                    move,
                    state.AmplitudeScale,
                    state.StartFlipDirection);
            }

            state.RestRelativeHuntPos[handIndex] = rest;
            state.RaisedRelativeHuntPos[handIndex] = raised;
        }

        if (state.ActivePose == TwirlPose.Crawl)
        {
            state.CrawlAnchorLogged = true;
        }

        state.HandAnchorsCaptured = true;
    }

    internal static void RefreshRestFromHands(PlayerGraphics graphics, PlayerTwirlState.Data state)
    {
        for (var handIndex = 0; handIndex < SpearHandMask.HandCount; handIndex++)
        {
            if (!SpearHandMask.Includes(state.StartSpearHandMask, handIndex))
            {
                continue;
            }

            state.RestRelativeHuntPos[handIndex] = graphics.hands[handIndex].relativeHuntPos;
        }
    }

    internal static void ResetAnchors(PlayerTwirlState.Data state)
    {
        state.HandAnchorsCaptured = false;
        state.LookCaptured = false;
        state.CrawlAnchorLogged = false;
        state.PoseTransitionRemaining = 0;
        state.LockedLookDirection = Vector2.zero;
        state.ActiveSpinFrames = 0;
        state.ActiveSpinRotations = 0f;
        state.TransitionFromRelative[0] = Vector2.zero;
        state.TransitionFromRelative[1] = Vector2.zero;
        state.RestRelativeHuntPos[0] = Vector2.zero;
        state.RestRelativeHuntPos[1] = Vector2.zero;
        state.RaisedRelativeHuntPos[0] = Vector2.zero;
        state.RaisedRelativeHuntPos[1] = Vector2.zero;
    }

    internal static void ClearSpearDir(PlayerGraphics graphics) => graphics.spearDir = 0f;

    internal static void ClearSpearDir(Player player)
    {
        if (player.graphicsModule is PlayerGraphics graphics)
        {
            ClearSpearDir(graphics);
        }
    }

    private static Vector2 ComputeWristOffset(PlayerTwirlState.Data state, TwirlMoves.Definition move)
    {
        if (state.CurrentSegment != PlayerTwirlState.Segment.Spin)
        {
            return Vector2.zero;
        }

        var phase = TwirlSession.SpinPhaseAngleRadians(state) * move.WristHarmonics;
        var amp = state.AmplitudeScale;
        var radiusY = UsesLowWristProfile(state.ActivePose)
            ? move.WristRadiusY * 0.45f
            : move.WristRadiusY;

        return new Vector2(
            Mathf.Cos(phase) * move.WristRadiusX * amp,
            Mathf.Sin(phase) * radiusY * amp);
    }

    private static void ApplyLookDirection(
        PlayerGraphics graphics,
        PlayerTwirlState.Data state,
        Vector2? spearHandPos)
    {
        if (spearHandPos.HasValue && ShouldFollowSpearHand(state))
        {
            var target = Custom.DirVec(graphics.head.pos, spearHandPos.Value);
            if (state.CurrentSegment == PlayerTwirlState.Segment.Spin)
            {
                graphics.lookDirection = target;
                return;
            }

            if (UsesFixedAnchors(state.ActivePose))
            {
                graphics.lookDirection = target;
                return;
            }
        }

        if (spearHandPos.HasValue && Custom.Dist(graphics.head.pos, spearHandPos.Value) > 1f)
        {
            graphics.lookDirection = Custom.DirVec(graphics.head.pos, spearHandPos.Value);
            return;
        }

        if (state.LockedLookDirection.sqrMagnitude > 0.001f)
        {
            graphics.lookDirection = state.LockedLookDirection;
        }
    }

    private static bool ShouldFollowSpearHand(PlayerTwirlState.Data state) =>
        state.CurrentSegment == PlayerTwirlState.Segment.Spin
        || UsesFixedAnchors(state.ActivePose);

    private static bool UsesFixedAnchors(TwirlPose pose) =>
        pose == TwirlPose.Crawl
        || pose == TwirlPose.Slide
        || pose == TwirlPose.BellySlide;

    private static bool UsesLowWristProfile(TwirlPose pose) =>
        pose == TwirlPose.Crawl
        || pose == TwirlPose.Slide
        || pose == TwirlPose.BellySlide;

    private static Vector2 ResolveFixedGrasp(TwirlPose pose, int handIndex, int flipDirection) =>
        pose switch
        {
            TwirlPose.Crawl => TwirlMoves.CrawlGraspRelative(handIndex, flipDirection),
            TwirlPose.Slide => TwirlMoves.SlideGraspRelative(handIndex, flipDirection),
            TwirlPose.BellySlide => TwirlMoves.BellySlideGraspRelative(handIndex, flipDirection),
            _ => TwirlMoves.DefaultGraspRelative(pose, handIndex),
        };

    private static float ResolveHandHuntSpeed(TwirlPose pose) => pose switch
    {
        TwirlPose.Crawl => 12f,
        TwirlPose.Slide => 11f,
        TwirlPose.BellySlide => 10f,
        _ => 10f,
    };

    private static float ResolveHandQuickness(TwirlPose pose) => pose switch
    {
        TwirlPose.Airborne => 0.45f,
        TwirlPose.Slide => 0.42f,
        TwirlPose.BellySlide => 0.4f,
        _ => 0.35f,
    };

    private static Vector2 RelativeHuntToWorld(SlugcatHand hand, Vector2 relative)
    {
        var connection = hand.connection;
        var angle = Custom.AimFromOneVectorToAnother(connection.rotationChunk.pos, connection.pos);
        return connection.pos + Custom.RotateAroundOrigo(relative, angle);
    }
}
