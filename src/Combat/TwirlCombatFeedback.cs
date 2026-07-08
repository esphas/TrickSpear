using RWCustom;
using UnityEngine;

namespace TrickSpear;

internal static class TwirlCombatFeedback
{
    internal static void PlayParry(Room? room, Vector2 pos)
    {
        if (room == null)
        {
            return;
        }

        room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, pos);
        room.AddObject(new ExplosionSpikes(
            room,
            pos,
            8,
            30f,
            7f,
            7f,
            60f,
            new Color(1f, 1f, 1f, 0.8f)));
    }
}
