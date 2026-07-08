using BepInEx.Logging;
using MonoMod.RuntimeDetour;
using RainMeadow;
using System;
using System.Reflection;

namespace TrickSpear;

internal static class TwirlMeadowIntegration
{
    private static Hook? _newEntityHook;
    private static ManualLogSource? _logger;

    internal static void Install(ManualLogSource logger)
    {
        _logger = logger;

        TwirlNetworkGuard.Register(
            () => OnlineManager.lobby != null,
            player => player.abstractCreature.IsLocal(),
            UpdateRemoteMirror,
            EnsureEntityData);

        TwirlMeadowCombat.BindRpcMethods(logger);

        var target = typeof(OnlineGameMode).GetMethod(
            nameof(OnlineGameMode.NewEntity),
            BindingFlags.Public | BindingFlags.Instance);
        if (target == null)
        {
            throw new MissingMethodException(nameof(OnlineGameMode), nameof(OnlineGameMode.NewEntity));
        }

        _newEntityHook = new Hook(target, NewEntityHook);
        logger.LogInfo("TrickSpear Rain Meadow integration active");
    }

    private static void NewEntityHook(
        Action<OnlineGameMode, OnlineEntity, OnlineResource> orig,
        OnlineGameMode self,
        OnlineEntity oe,
        OnlineResource resource)
    {
        orig(self, oe, resource);
        TryRegisterTwirlData(oe);
    }

    internal static void EnsureEntityData(Player player)
    {
        if (player?.abstractCreature == null)
        {
            return;
        }

        var oc = player.abstractCreature.GetOnlineCreature();
        if (oc == null)
        {
            return;
        }

        if (!oc.TryGetData<TwirlNetworkData>(out _))
        {
            oc.AddData(new TwirlNetworkData());
        }
    }

    private static void TryRegisterTwirlData(OnlineEntity oe)
    {
        if (oe is not OnlineCreature oc)
        {
            return;
        }

        if (oc.TryGetData<TwirlNetworkData>(out _))
        {
            return;
        }

        if (oc.abstractCreature?.state is PlayerState)
        {
            oc.AddData(new TwirlNetworkData());
        }
    }

    private static bool UpdateRemoteMirror(Player player)
    {
        var state = PlayerTwirlState.Get(player);

        if (!state.IsTwirling)
        {
            TwirlSpinFeedback.StopAll(state);
            return true;
        }

        TwirlPoseTransition.Update(player, state);
        TwirlSpinFeedback.Update(player, state);
        return true;
    }
}
