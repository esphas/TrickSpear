using System.Runtime.CompilerServices;
using UnityEngine;

namespace TrickSpear;

internal static class PlayerTwirlState
{
    internal enum Segment
    {
        Raise,
        Spin,
        Lower,
    }

    internal sealed class Data
    {
        public bool IsTwirling;
        public bool InHoldSession;

        public int MoveIndex;
        public Segment CurrentSegment = Segment.Raise;
        public int SegmentTimer;
        public int ActiveRaiseFrames = Timing.RaiseFrames;

        public int SpinElapsed;
        public int ActiveSpinFrames;
        public float ActiveSpinRotations;

        public float AmplitudeScale = 1f;
        public float SpeedScale = 1f;
        public int ScaledRaiseFrames = Timing.RaiseFrames;
        public int ScaledChainRaiseFrames = Timing.ChainRaiseFrames;
        public int ScaledLowerFrames = Timing.LowerFrames;

        public TwirlPose StartPose = TwirlPose.Stand;
        public TwirlPose ActivePose = TwirlPose.Stand;
        public int StartFlipDirection;
        public float StartHoldAngleDeg;

        public int PoseTransitionRemaining;
        public readonly Vector2[] TransitionFromRelative = { Vector2.zero, Vector2.zero };

        public int StartSpearHandMask;

        public bool HandAnchorsCaptured;
        public readonly Vector2[] RestRelativeHuntPos = { Vector2.zero, Vector2.zero };
        public readonly Vector2[] RaisedRelativeHuntPos = { Vector2.zero, Vector2.zero };

        public bool LookCaptured;
        public Vector2 LockedLookDirection;

        public bool CrawlAnchorLogged;

        public float SpinPreviousPhaseRad;

        public TwirlRotationTrail? SpinTrail;

        public ChunkSoundEmitter? SpinLoopEmitter;

        public TwirlAutoTriggerKind AutoTriggerKind;
    }

    internal static class Timing
    {
        internal const int RaiseFrames = 6;
        internal const int ChainRaiseFrames = 4;
        internal const int LowerFrames = 5;
    }

    private static readonly ConditionalWeakTable<Player, Data> Table = new();

    internal static Data Get(Player player) => Table.GetOrCreateValue(player);
}
