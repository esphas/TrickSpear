namespace TrickSpear;

internal static class TwirlDebug
{
    internal static bool Enabled { get; set; }

    private static int _lastRejectLogFrame = -9999;

    internal static void LogTwirlStart(Player player, PlayerTwirlState.Data state, bool chained)
    {
        if (!Enabled)
        {
            return;
        }

        TrickSpearPlugin.Logger.LogInfo(
            $"TrickSpear [twirl] pose={state.ActivePose} chained={chained} " +
            $"body={player.bodyMode} standing={player.standing} anim={player.animation} " +
            $"seg={state.CurrentSegment} move={state.MoveIndex} mask={state.StartSpearHandMask}");
    }

    internal static void LogStartRejected(Player player, TwirlInput.Snapshot snap)
    {
        if (!Enabled || !snap.JustPressed)
        {
            return;
        }

        var frame = UnityEngine.Time.frameCount;
        if (frame - _lastRejectLogFrame < 30)
        {
            return;
        }

        _lastRejectLogFrame = frame;
        var reason = GameplayGuards.DescribeStartBlock(player);
        TrickSpearPlugin.Logger.LogInfo(
            $"TrickSpear [reject] reason={reason} body={player.bodyMode} " +
            $"standing={player.standing} anim={player.animation} spear={SpearChecks.HasSpearInHand(player)}");
    }

    internal static void LogCrawlAnchors(int handIndex, UnityEngine.Vector2 rest, UnityEngine.Vector2 raised)
    {
        if (!Enabled)
        {
            return;
        }

        TrickSpearPlugin.Logger.LogInfo(
            $"TrickSpear [crawl-anchor] hand={handIndex} rest=({rest.x:F1},{rest.y:F1}) " +
            $"raised=({raised.x:F1},{raised.y:F1})");
    }

    internal static void LogHardAbort(string reason, int currentMask, int startMask, PlayerTwirlState.Data state)
    {
        if (!Enabled)
        {
            return;
        }

        TrickSpearPlugin.Logger.LogInfo(
            $"TrickSpear [hard-abort] reason={reason} mask={currentMask}/{startMask} " +
            $"pose={state.ActivePose} seg={state.CurrentSegment}");
    }

    internal static void LogSegmentAdvance(
        PlayerTwirlState.Segment from,
        string action,
        PlayerTwirlState.Data state)
    {
        if (!Enabled)
        {
            return;
        }

        TrickSpearPlugin.Logger.LogInfo(
            $"TrickSpear [segment] {from}->{action} pose={state.ActivePose} " +
            $"move={state.MoveIndex} next={state.CurrentSegment} timer={state.SegmentTimer}");
    }

    internal static void LogSpinSfx(string message)
    {
        if (!Enabled)
        {
            return;
        }

        TrickSpearPlugin.Logger.LogInfo($"TrickSpear [sfx] {message}");
    }

    internal static void LogAutoTwirl(Player player, TwirlAutoTriggerKind kind)
    {
        if (!Enabled)
        {
            return;
        }

        TrickSpearPlugin.Logger.LogInfo(
            $"TrickSpear [auto-twirl] kind={kind} anim={player.animation} " +
            $"slide={player.slideCounter} standing={player.standing} body={player.bodyMode}");
    }
}
