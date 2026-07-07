namespace TrickSpear;

internal static class SpearHandMask
{
    internal const int HandCount = 2;

    internal static bool Includes(int mask, int handIndex) => (mask & (1 << handIndex)) != 0;

    internal static bool IsUnchanged(int currentMask, int startMask) => currentMask == startMask;
}
