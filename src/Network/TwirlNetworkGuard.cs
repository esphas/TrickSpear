using System;

namespace TrickSpear;

internal static class TwirlNetworkGuard
{
    private static Func<bool>? _isOnlineSession;
    private static Func<Player, bool>? _isLocalPlayer;
    private static Func<Player, bool>? _handleRemoteUpdate;
    private static Action<Player>? _ensureEntityData;

    internal static bool MeadowIntegrationActive { get; private set; }

    internal static void Register(
        Func<bool> isOnlineSession,
        Func<Player, bool> isLocalPlayer,
        Func<Player, bool> handleRemoteUpdate,
        Action<Player> ensureEntityData)
    {
        _isOnlineSession = isOnlineSession;
        _isLocalPlayer = isLocalPlayer;
        _handleRemoteUpdate = handleRemoteUpdate;
        _ensureEntityData = ensureEntityData;
        MeadowIntegrationActive = true;
    }

    internal static bool IsOnlineSession => MeadowIntegrationActive && (_isOnlineSession?.Invoke() ?? false);

    internal static bool IsLocalPlayer(Player player) =>
        !MeadowIntegrationActive || (_isLocalPlayer?.Invoke(player) ?? true);

    internal static bool ShouldUseRemoteMirror(Player player) =>
        IsOnlineSession && !IsLocalPlayer(player);

    internal static bool AllowCombatPhysics =>
        !IsOnlineSession || TwirlCombatConfig.OnlineCombatExperimental;

    internal static bool AllowCombatForPlayer(Player player) =>
        AllowCombatPhysics && IsLocalPlayer(player);

    internal static void EnsureEntityData(Player player)
    {
        if (!IsOnlineSession)
        {
            return;
        }

        _ensureEntityData?.Invoke(player);
    }

    internal static bool TryHandleRemoteUpdate(Player player)
    {
        if (!ShouldUseRemoteMirror(player))
        {
            return false;
        }

        return _handleRemoteUpdate?.Invoke(player) ?? false;
    }
}
