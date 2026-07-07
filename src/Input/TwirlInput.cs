using ImprovedInput;
using System.Runtime.CompilerServices;

namespace TrickSpear;

internal static class TwirlInput
{
    internal readonly struct Snapshot
    {
        internal readonly bool JustPressed;
        internal readonly bool JustReleased;
        internal readonly bool IicPressed;
        internal readonly bool RawPressed;
        internal readonly bool CheckRaw;
        internal readonly bool Held;
        internal readonly bool SessionActive;
        internal readonly int PlayerNumber;

        internal Snapshot(
            bool justPressed,
            bool justReleased,
            bool iicPressed,
            bool rawPressed,
            bool checkRaw,
            bool held,
            bool sessionActive,
            int playerNumber)
        {
            JustPressed = justPressed;
            JustReleased = justReleased;
            IicPressed = iicPressed;
            RawPressed = rawPressed;
            CheckRaw = checkRaw;
            Held = held;
            SessionActive = sessionActive;
            PlayerNumber = playerNumber;
        }
    }

    private sealed class LatchData
    {
        public bool SessionActive;
        public Snapshot LastSnapshot;
    }

    private static readonly ConditionalWeakTable<Player, LatchData> Latches = new();

    internal static void RefreshAfterCheckInput(Player player)
    {
        var latch = Latches.GetOrCreateValue(player);
        latch.LastSnapshot = BuildSnapshot(player, latch);
    }

    internal static Snapshot GetSnapshot(Player player) =>
        Latches.GetOrCreateValue(player).LastSnapshot;

    internal static void ForceEndSession(Player player)
    {
        var latch = Latches.GetOrCreateValue(player);
        latch.SessionActive = false;
        latch.LastSnapshot = BuildSnapshot(player, latch);
    }

    private static Snapshot BuildSnapshot(Player player, LatchData latch)
    {
        var key = TrickSpearPlugin.TwirlKeybind;
        var playerNumber = player.playerState.playerNumber;

        var iicHistory = CustomInputExt.InputHistory(player);
        var rawHistory = CustomInputExt.RawInputHistory(player);

        var iicPressed = CustomInputExt.IsPressed(player, key);
        var rawPressed = CustomInputExt.RawInput(player)[key];

        var checkRaw = false;
        if (playerNumber >= 0 && playerNumber < CustomInputExt.MaxPlayers)
        {
            checkRaw = key.CheckRawPressed(playerNumber);
        }

        var justPressed = EdgePressed(iicHistory, key) || EdgePressed(rawHistory, key);
        var justReleased = EdgeReleased(iicHistory, key) || EdgeReleased(rawHistory, key);
        var held = iicPressed || rawPressed || checkRaw;

        if (justPressed)
        {
            latch.SessionActive = true;
        }
        else if (justReleased)
        {
            latch.SessionActive = false;
        }

        return new Snapshot(
            justPressed,
            justReleased,
            iicPressed,
            rawPressed,
            checkRaw,
            held,
            latch.SessionActive,
            playerNumber);
    }

    private static bool EdgePressed(CustomInput[] history, PlayerKeybind key) =>
        history.Length > 1 && history[0][key] && !history[1][key];

    private static bool EdgeReleased(CustomInput[] history, PlayerKeybind key) =>
        history.Length > 1 && !history[0][key] && history[1][key];
}
