namespace TrickSpear;

internal static class TwirlNetworkMirror
{
    internal static void Apply(Player player, TwirlNetworkData.State net)
    {
        var state = PlayerTwirlState.Get(player);
        var wasTwirling = state.IsTwirling;
        var prevSegment = state.CurrentSegment;

        CopyToState(state, net);

        if (state.IsTwirling && !wasTwirling)
        {
            state.HandAnchorsCaptured = false;
            TwirlPhaseSync.Reset(state);
        }

        if (state.IsTwirling
            && state.CurrentSegment == PlayerTwirlState.Segment.Spin
            && prevSegment != PlayerTwirlState.Segment.Spin)
        {
            TwirlSpinFeedback.OnEnterSpin(player, state);
        }

        if (!state.IsTwirling && wasTwirling)
        {
            TwirlSpinFeedback.StopAll(state);
            TwirlVisuals.ClearSpearDir(player);
        }
    }

    private static void CopyToState(PlayerTwirlState.Data state, TwirlNetworkData.State net)
    {
        state.IsTwirling = net.isTwirling;
        state.InHoldSession = net.inHoldSession;
        state.CurrentSegment = (PlayerTwirlState.Segment)net.currentSegment;
        state.SegmentTimer = net.segmentTimer;
        state.SpinElapsed = net.spinElapsed;
        state.MoveIndex = net.moveIndex;
        state.ActivePose = (TwirlPose)net.activePose;
        state.StartPose = (TwirlPose)net.startPose;
        state.StartFlipDirection = net.startFlipDirection;
        state.StartHoldAngleDeg = net.startHoldAngleDeg;
        state.AmplitudeScale = net.amplitudeScale;
        state.SpeedScale = net.speedScale;
        state.StartSpearHandMask = net.startSpearHandMask;
        state.ActiveSpinFrames = net.activeSpinFrames;
        state.ActiveRaiseFrames = net.activeRaiseFrames;
        state.ActiveSpinRotations = net.activeSpinRotations;
        state.ScaledRaiseFrames = net.scaledRaiseFrames;
        state.ScaledChainRaiseFrames = net.scaledChainRaiseFrames;
        state.ScaledLowerFrames = net.scaledLowerFrames;
        state.AutoTriggerKind = (TwirlAutoTriggerKind)net.autoTriggerKind;
        state.PoseTransitionRemaining = net.poseTransitionRemaining;
    }
}
