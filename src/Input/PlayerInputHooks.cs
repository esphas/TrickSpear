using System;

namespace TrickSpear;

internal static class PlayerInputHooks
{
    private static bool _loggedRefreshError;

    internal static void CheckInput(On.Player.orig_checkInput orig, Player self)
    {
        orig(self);

        if (self.isNPC)
        {
            return;
        }

        try
        {
            TrickSpearPlugin.TryLogKeybindStatus(self);
            TwirlInput.RefreshAfterCheckInput(self);
        }
        catch (Exception e)
        {
            if (!_loggedRefreshError)
            {
                _loggedRefreshError = true;
                TrickSpearPlugin.Logger.LogError($"TrickSpear checkInput refresh failed: {e}");
            }
        }
    }
}
