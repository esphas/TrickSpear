using BepInEx.Logging;
using RainMeadow;
using System;
using System.Reflection;

namespace TrickSpear;

internal static class TwirlMeadowBindings
{
    private static Action<OnlineEntity, string, RPCEvent>? _lockEntity;

    internal static Action<OnlinePhysicalObject, RealizedWeaponState, bool, bool>? WeaponCreatureDeflect { get; private set; }

    internal static void Install(ManualLogSource logger)
    {
        BindLock(logger);
        BindWeaponCreatureDeflect(logger);
    }

    internal static void LockEntity(OnlineEntity entity, string key, RPCEvent rpcEvent)
    {
        _lockEntity?.Invoke(entity, key, rpcEvent);
    }

    private static void BindLock(ManualLogSource logger)
    {
        var method = typeof(OnlineEntity).GetMethod(
            "Lock",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(string), typeof(RPCEvent) },
            modifiers: null);
        if (method == null)
        {
            logger.LogWarning("TrickSpear could not bind Rain Meadow OnlineEntity.Lock");
            return;
        }

        _lockEntity = (Action<OnlineEntity, string, RPCEvent>)Delegate.CreateDelegate(
            typeof(Action<OnlineEntity, string, RPCEvent>),
            method);
    }

    private static void BindWeaponCreatureDeflect(ManualLogSource logger)
    {
        var method = typeof(RPCs).GetMethod(
            "Weapon_CreatureDeflect",
            BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            logger.LogWarning("TrickSpear could not bind Rain Meadow Weapon_CreatureDeflect RPC");
            return;
        }

        WeaponCreatureDeflect = (Action<OnlinePhysicalObject, RealizedWeaponState, bool, bool>)Delegate.CreateDelegate(
            typeof(Action<OnlinePhysicalObject, RealizedWeaponState, bool, bool>),
            method);
    }
}
