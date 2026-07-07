namespace TrickSpear;

internal static class SpearHooks
{
    internal static void Update(On.Spear.orig_Update orig, Spear self, bool eu)
    {
        orig(self, eu);

        if (!TwirlContext.TryGetSpearOwner(self, out var player))
        {
            return;
        }

        var state = PlayerTwirlState.Get(player);
        if (!state.IsTwirling || !SpearChecks.IsHeldSpear(self, player))
        {
            return;
        }

        var orientation = TwirlSession.SpearOrientation(state);
        self.rotation = orientation;
        self.setRotation = orientation;
        self.rotationSpeed = 0f;
    }
}
