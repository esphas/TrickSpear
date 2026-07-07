using UnityEngine;

namespace TrickSpear;

internal static class TwirlSfx
{
    internal const float StartVolume = 0.3f;
    internal const float StartPitch = 1.15f;

    internal const float SpinLoopVolume = 0.55f;
    internal const float SpinSwishVolume = 0.34f;

    internal static void PlayStart(Player player)
    {
        if (player.room == null)
        {
            return;
        }

        PlaySwish(player, StartVolume, StartPitch);
    }

    internal static void OnEnterSpin(Player player, PlayerTwirlState.Data state)
    {
        StopSpinLoop(state);
        var pitch = 1.05f + TwirlPhaseSync.MovePitchOffset(state);
        PlaySwish(player, SpinSwishVolume * TwirlPhaseSync.MoveAccent(state), pitch);
        TwirlDebug.LogSpinSfx($"enter-spin move={state.MoveIndex} pitch={pitch:F2}");
    }

    internal static void UpdateLoop(Player player, PlayerTwirlState.Data state)
    {
        EnsureSpinLoop(player, state);
        MaintainSpinLoop(state);
    }

    internal static void PlayHalfRevSwish(Player player, PlayerTwirlState.Data state, int crossingIndex)
    {
        var progress = state.ActiveSpinFrames <= 0
            ? 0f
            : state.SpinElapsed / (float)state.ActiveSpinFrames;
        var pitch = Mathf.Lerp(1f, 1.32f, progress) + TwirlPhaseSync.MovePitchOffset(state);
        var volume = SpinSwishVolume * TwirlPhaseSync.MoveAccent(state);
        PlaySwish(player, volume, pitch);
        TwirlDebug.LogSpinSfx($"swish half-rev={crossingIndex} move={state.MoveIndex} pitch={pitch:F2}");
    }

    internal static void StopAll(PlayerTwirlState.Data state)
    {
        StopLoop(state);
        TwirlPhaseSync.Reset(state);
    }

    internal static void StopLoop(PlayerTwirlState.Data state)
    {
        StopSpinLoop(state);
    }

    private static void EnsureSpinLoop(Player player, PlayerTwirlState.Data state)
    {
        if (state.SpinLoopEmitter != null && state.SpinLoopEmitter.alive)
        {
            return;
        }

        var chunk = SpearChecks.GetSpearBodyChunk(player) ?? player.mainBodyChunk;
        var volume = SpinLoopVolume * TwirlPhaseSync.MoveAccent(state);
        state.SpinLoopEmitter = player.room.PlaySound(
            SoundID.Spear_Spinning_Through_Air_LOOP,
            chunk,
            loop: true,
            volume,
            1f);
        state.SpinLoopEmitter.requireActiveUpkeep = true;
        TwirlDebug.LogSpinSfx($"loop-start move={state.MoveIndex} vol={volume:F2}");
    }

    private static void MaintainSpinLoop(PlayerTwirlState.Data state)
    {
        var emitter = state.SpinLoopEmitter;
        if (emitter == null)
        {
            return;
        }

        emitter.alive = true;
        var progress = state.ActiveSpinFrames <= 0
            ? 0f
            : state.SpinElapsed / (float)state.ActiveSpinFrames;
        var accent = TwirlPhaseSync.MoveAccent(state);
        emitter.pitch = Mathf.Lerp(0.95f, 1.28f, progress) + TwirlPhaseSync.MovePitchOffset(state);
        emitter.volume = SpinLoopVolume * accent * (0.85f + 0.15f * Mathf.Sin(progress * Mathf.PI));
    }

    private static void PlaySwish(Player player, float volume, float pitch)
    {
        if (player.room == null)
        {
            return;
        }

        var chunk = SpearChecks.GetSpearBodyChunk(player) ?? player.mainBodyChunk;
        player.room.PlaySound(
            SoundID.Slugcat_Throw_Spear,
            chunk,
            false,
            volume,
            pitch);
    }

    private static void StopSpinLoop(PlayerTwirlState.Data state)
    {
        if (state.SpinLoopEmitter == null)
        {
            return;
        }

        state.SpinLoopEmitter.alive = false;
        state.SpinLoopEmitter = null;
        TwirlDebug.LogSpinSfx("loop-stop");
    }
}
