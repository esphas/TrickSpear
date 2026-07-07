using RWCustom;
using UnityEngine;

namespace TrickSpear;

internal static class TwirlSession
{
    internal static bool Begin(Player player, PlayerTwirlState.Data state, bool chained, TwirlAutoTriggerKind autoKind = TwirlAutoTriggerKind.None)
    {
        var pose = state.ActivePose;
        if (!chained)
        {
            var resolved = autoKind != TwirlAutoTriggerKind.None
                ? TwirlPoseProfiles.ResolveAutoTriggerPose(player, autoKind)
                : TwirlPoseProfiles.ResolveStartPose(player);
            if (resolved == null)
            {
                return false;
            }

            pose = resolved.Value;
            state.AutoTriggerKind = autoKind;
        }

        state.IsTwirling = true;
        state.StartPose = pose;
        state.ActivePose = pose;
        state.MoveIndex = chained
            ? (state.MoveIndex + 1) % TwirlPoseProfiles.SequenceCount(pose)
            : 0;
        state.CurrentSegment = PlayerTwirlState.Segment.Raise;
        state.SpinElapsed = 0;
        state.PoseTransitionRemaining = 0;

        if (!chained)
        {
            TwirlVisuals.ResetAnchors(state);
            TwirlVariation.RollSessionParams(player, state);
            state.StartFlipDirection = player.flipDirection;
            state.StartHoldAngleDeg = TwirlMoves.ResolveStartHoldAngleDeg(player, pose);
            state.StartSpearHandMask = SpearChecks.GetSpearHandMask(player);
            state.ActiveRaiseFrames = state.ScaledRaiseFrames;
        }
        else
        {
            state.ActiveRaiseFrames = state.ScaledChainRaiseFrames;
        }

        state.SegmentTimer = state.ActiveRaiseFrames;
        return true;
    }

    internal static bool AdvanceSegment(Player player, PlayerTwirlState.Data state, PlayerTwirlState.Segment fromSegment)
    {
        switch (state.CurrentSegment)
        {
            case PlayerTwirlState.Segment.Raise:
                EnterSpin(player, state);
                TwirlDebug.LogSegmentAdvance(fromSegment, "Spin", state);
                return true;

            case PlayerTwirlState.Segment.Spin:
                if (TryChain(player, state, fromSegment))
                {
                    return true;
                }

                state.CurrentSegment = PlayerTwirlState.Segment.Lower;
                state.SegmentTimer = state.ScaledLowerFrames;
                TwirlDebug.LogSegmentAdvance(fromSegment, "Lower", state);
                return true;

            case PlayerTwirlState.Segment.Lower:
                if (TryChain(player, state, fromSegment))
                {
                    return true;
                }

                TwirlDebug.LogSegmentAdvance(fromSegment, "End", state);
                return false;

            default:
                return false;
        }
    }

    internal static void Tick(PlayerTwirlState.Data state)
    {
        TwirlPoseTransition.Tick(state);

        if (state.CurrentSegment == PlayerTwirlState.Segment.Spin)
        {
            state.SpinElapsed++;
        }
    }

    internal static void Finish(PlayerTwirlState.Data state, bool endSession)
    {
        TwirlSpinFeedback.StopAll(state);
        state.AutoTriggerKind = TwirlAutoTriggerKind.None;
        state.IsTwirling = false;
        state.SegmentTimer = 0;
        state.SpinElapsed = 0;
        state.CurrentSegment = PlayerTwirlState.Segment.Raise;

        if (endSession)
        {
            state.InHoldSession = false;
            state.MoveIndex = 0;
            ResetSession(state);
        }
    }

    internal static void ResetSession(PlayerTwirlState.Data state)
    {
        TwirlVisuals.ResetAnchors(state);
        state.StartSpearHandMask = 0;
        state.StartPose = TwirlPose.Stand;
        state.ActivePose = TwirlPose.Stand;
        state.AmplitudeScale = 1f;
        state.SpeedScale = 1f;
        state.ScaledRaiseFrames = PlayerTwirlState.Timing.RaiseFrames;
        state.ScaledChainRaiseFrames = PlayerTwirlState.Timing.ChainRaiseFrames;
        state.ScaledLowerFrames = PlayerTwirlState.Timing.LowerFrames;
    }

    internal static float GetLiftBlend(PlayerTwirlState.Data state) =>
        state.CurrentSegment switch
        {
            PlayerTwirlState.Segment.Raise => TwirlMath.SmoothStep(Mathf.Clamp01(
                TwirlMath.SegmentProgress(state.SegmentTimer, state.ActiveRaiseFrames))),
            PlayerTwirlState.Segment.Lower => GetLowerBlend(state),
            _ => 1f,
        };

    private static float GetLowerBlend(PlayerTwirlState.Data state)
    {
        var progress = Mathf.Clamp01(
            TwirlMath.SegmentProgress(state.SegmentTimer, state.ScaledLowerFrames));
        return TwirlMath.EaseInQuad(1f - progress);
    }

    internal static float SpinPhaseAngleRadians(PlayerTwirlState.Data state)
    {
        var t = Mathf.Clamp01(state.SpinElapsed / (float)state.ActiveSpinFrames);
        return t * state.ActiveSpinRotations * Mathf.PI * 2f;
    }

    internal static Vector2 SpearOrientation(PlayerTwirlState.Data state)
    {
        if (state.CurrentSegment != PlayerTwirlState.Segment.Spin)
        {
            return Custom.DegToVec(state.StartHoldAngleDeg).normalized;
        }

        var phaseDeg = SpinPhaseAngleRadians(state) * Mathf.Rad2Deg;
        return Custom.DegToVec(state.StartHoldAngleDeg + phaseDeg).normalized;
    }

    internal static TwirlMoves.Definition ActiveMove(PlayerTwirlState.Data state) =>
        TwirlMoves.Get(state.ActivePose, state.MoveIndex);

    private static bool TryChain(Player player, PlayerTwirlState.Data state, PlayerTwirlState.Segment fromSegment)
    {
        if (!state.InHoldSession)
        {
            return false;
        }

        ChainToNextMove(player, state);
        TwirlDebug.LogSegmentAdvance(fromSegment, "Chain", state);
        return true;
    }

    private static void EnterSpin(Player player, PlayerTwirlState.Data state)
    {
        var move = ActiveMove(state);
        state.CurrentSegment = PlayerTwirlState.Segment.Spin;
        state.ActiveSpinRotations = TwirlVariation.ResolveActiveSpinRotations(
            move.Rotations,
            state.InHoldSession);
        state.ActiveSpinFrames = TwirlVariation.ResolveActiveSpinFrames(
            move.SpinFrames,
            state.SpeedScale,
            state.ActiveSpinRotations,
            move.Rotations);
        state.SegmentTimer = state.ActiveSpinFrames;
        state.SpinElapsed = 0;
        TwirlSpinFeedback.OnEnterSpin(player, state);
    }

    private static void ChainToNextMove(Player player, PlayerTwirlState.Data state)
    {
        if (player.graphicsModule is PlayerGraphics graphics)
        {
            TwirlVisuals.RefreshRestFromHands(graphics, state);
        }

        state.MoveIndex = (state.MoveIndex + 1) % TwirlPoseProfiles.SequenceCount(state.ActivePose);
        var move = ActiveMove(state);

        for (var handIndex = 0; handIndex < SpearHandMask.HandCount; handIndex++)
        {
            if (!SpearHandMask.Includes(state.StartSpearHandMask, handIndex))
            {
                continue;
            }

            state.RaisedRelativeHuntPos[handIndex] = TwirlMoves.ComputeRaisedRelative(
                state.ActivePose,
                state.RestRelativeHuntPos[handIndex],
                handIndex,
                move,
                state.AmplitudeScale,
                state.StartFlipDirection);
        }

        state.CurrentSegment = PlayerTwirlState.Segment.Raise;
        state.ActiveRaiseFrames = state.ScaledChainRaiseFrames;
        state.SegmentTimer = state.ActiveRaiseFrames;
        state.SpinElapsed = 0;
        state.HandAnchorsCaptured = true;
    }
}
