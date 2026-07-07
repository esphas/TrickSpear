namespace TrickSpear;

internal static class PlayerGraphicsHooks
{
    internal static void Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
    {
        orig(self);

        if (self.owner is not Player player || player.isNPC || player.room == null)
        {
            return;
        }

        var state = PlayerTwirlState.Get(player);
        if (!TwirlContext.IsGraphicsReady(player, state))
        {
            return;
        }

        TwirlVisuals.Apply(self, player, state);
    }
}
