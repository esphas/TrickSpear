namespace TrickSpear;

internal static class PlayerHooks
{
    internal static void Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        if (self.isNPC)
        {
            return;
        }

        var state = PlayerTwirlState.Get(self);

        if (!GameplayGuards.IsInGameplaySession(self))
        {
            if (state.IsTwirling)
            {
                EndTwirl(self, state, endSession: true);
            }

            return;
        }

        TwirlNetworkGuard.EnsureEntityData(self);

        if (!TwirlNetworkGuard.TryHandleRemoteUpdate(self))
        {
            var snap = TwirlInput.GetSnapshot(self);
            state.InHoldSession = snap.SessionActive;
            if (!state.IsTwirling) TryStartTwirl(self, state, snap);     
        }

        if (state.IsTwirling)
        {
            UpdateWhileTwirling(self, state);
            return;
        }
        
           
        TwirlAutoTrigger.TryFire(self, state);
    }

    private static void UpdateWhileTwirling(Player self, PlayerTwirlState.Data state)
    {
        var abortReason = TwirlInterrupts.GetHardAbortReason(self, state);
        if (abortReason != null)
        {
            TwirlDebug.LogHardAbort(
                abortReason,
                SpearChecks.GetSpearHandMask(self),
                state.StartSpearHandMask,
                state);
            EndTwirl(self, state, endSession: true);
            return;
        }

        TwirlInterrupts.ApplyDuringTwirl(self, state);

        // if (snap.Held)
        // {
        //     state.InHoldSession = true;
        // }

        TwirlPoseTransition.Update(self, state);
        TwirlSession.Tick(state);
        TwirlSpinFeedback.Update(self, state);
        SpinObjectInteract.Update(self, state);
        SpinParry.Update(self, state);
        state.SegmentTimer--;

        if (state.SegmentTimer > 0)
        {
            return;
        }

        var fromSegment = state.CurrentSegment;
        if (!TwirlSession.AdvanceSegment(self, state, fromSegment))
        {
            EndTwirl(self, state, endSession: !state.InHoldSession);
        }
    }

    private static void TryStartTwirl(Player self, PlayerTwirlState.Data state, TwirlInput.Snapshot snap)
    {
        TwirlDebug.LogStartRejected(self, snap);

        if (!GameplayGuards.CanStartTwirl(self))
        {
            return;
        }

        var startNewSession = snap.JustPressed;
        var resumeHeldSession = snap.SessionActive && snap.Held;

        if (!startNewSession && !resumeHeldSession)
        {
            return;
        }

        var chained = resumeHeldSession && !startNewSession;
        if (!TwirlSession.Begin(self, state, chained))
        {
            if (!snap.Held)
            {
                TwirlInput.ForceEndSession(self);
            }

            return;
        }

        TwirlDebug.LogTwirlStart(self, state, chained);

        if (startNewSession)
        {
            TwirlSfx.PlayStart(self);
        }
    }

    private static void EndTwirl(Player player, PlayerTwirlState.Data state, bool endSession)
    {
        TwirlSession.Finish(state, endSession);

        if (endSession)
        {
            TwirlInput.ForceEndSession(player);
        }

        TwirlVisuals.ClearSpearDir(player);
    }
}
