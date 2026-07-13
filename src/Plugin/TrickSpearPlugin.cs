using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;
using ImprovedInput;
using UnityEngine;

[assembly: AssemblyTitle("TrickSpear")]
[assembly: AssemblyDescription("Spear twirl atmospheric mod for Rain World")]
[assembly: AssemblyVersion("0.1.3")]
[assembly: AssemblyFileVersion("0.1.3")]
[assembly: ComVisible(false)]

namespace TrickSpear;

file static class PluginInfo
{
    internal const string Guid = "trick_spear";
    internal const string ModId = "trick_spear";
    internal const string Name = "Trick Spear";
    internal const string Version = "0.1.3";
}

[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
[BepInDependency("com.dual.improved-input-config", BepInDependency.DependencyFlags.HardDependency)]
public sealed class TrickSpearPlugin : BaseUnityPlugin
{
    internal static new BepInEx.Logging.ManualLogSource Logger { get; private set; } = null!;

    internal static PlayerKeybind? TwirlKeybind { get; private set; }

    private static bool _hooksApplied;
    private static bool _loggedKeybindStatus;
    private static bool _optionsRegistered;

    private void OnEnable()
    {
        Logger = base.Logger;
        On.RainWorld.OnModsInit += OnModsInit;

        try
        {
            TwirlKeybind = RegisterTwirlKeybind();
            Logger.LogInfo($"TrickSpear ready keybind={TwirlKeybind.Id}");
        }
        catch (Exception ex)
        {
            TwirlKeybind = null;
            Logger.LogError($"TrickSpear keybind register failed: {ex}");
        }

        ApplyHooks();
    }

    private void OnDisable()
    {
        On.RainWorld.OnModsInit -= OnModsInit;
    }

    private static void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        if (_optionsRegistered)
        {
            return;
        }

        try
        {
            var options = new TrickSpearOptions();
            if (MachineConnector.SetRegisteredOI(PluginInfo.ModId, options))
            {
                options.ApplyToRuntime();
                _optionsRegistered = true;
                Logger.LogInfo("TrickSpear Remix options registered");
            }
            else
            {
                Logger.LogWarning("TrickSpear SetRegisteredOI failed; is trick_spear enabled in Remix?");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"TrickSpear options init failed: {ex}");
        }

        if (ModManager.ActiveMods.Any(mod => mod.id == "henpemaz_rainmeadow"))
        {
            TwirlMeadowBootstrap.TryInstall(Logger);
        }
    }

    internal static void TryLogKeybindStatus(Player player)
    {
        if (_loggedKeybindStatus || player == null || TwirlKeybind == null)
        {
            return;
        }

        _loggedKeybindStatus = true;
        var pn = player.playerState.playerNumber;
        var bound = pn >= 0 && TwirlKeybind.Bound(pn);
        Logger.LogInfo($"TrickSpear keybind player={pn} bound={bound}");
    }

    private static PlayerKeybind RegisterTwirlKeybind()
    {
        const string id = "trickspear:twirl";
        var existing = PlayerKeybind.Get(id);
        if (existing != null)
        {
            return existing;
        }

        return PlayerKeybind.Register(
            id,
            PluginInfo.Name,
            LocKeys.InputTwirl,
            KeyCode.Q,
            KeyCode.JoystickButton4);
    }

    private static void ApplyHooks()
    {
        if (_hooksApplied)
        {
            return;
        }

        On.Player.checkInput += PlayerInputHooks.CheckInput;
        On.Player.Update += PlayerHooks.Update;
        On.Spear.Update += SpearHooks.Update;
        On.PlayerGraphics.Update += PlayerGraphicsHooks.Update;
        ParryHooks.Apply();
        _hooksApplied = true;
    }
}
