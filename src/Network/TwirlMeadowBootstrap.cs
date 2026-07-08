using BepInEx.Logging;
using System;
using System.Reflection;

namespace TrickSpear;

internal static class TwirlMeadowBootstrap
{
    internal static void TryInstall(ManualLogSource logger)
    {
        try
        {
            var integrationType = typeof(TwirlMeadowBootstrap).Assembly.GetType($"{typeof(TwirlMeadowBootstrap).Namespace}.TwirlMeadowIntegration", throwOnError: false);
            var install = integrationType?.GetMethod("Install", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (install == null)
            {
                logger.LogWarning("TrickSpear Meadow integration type not found");
                return;
            }

            install.Invoke(null, new object[] { logger });
        }
        catch (Exception ex)
        {
            logger.LogWarning($"TrickSpear Meadow bootstrap failed: {ex.Message}");
        }
    }
}
