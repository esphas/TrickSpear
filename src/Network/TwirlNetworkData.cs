using RainMeadow;
using System;

namespace TrickSpear;

internal sealed class TwirlNetworkData : OnlineEntity.EntityData
{
    public override EntityDataState MakeState(OnlineEntity onlineEntity, OnlineResource inResource)
    {
        if (inResource is RoomSession)
        {
            return new State(onlineEntity);
        }

        return null!;
    }

    internal sealed class State : EntityDataState
    {
        [OnlineField]
        public bool isTwirling;

        [OnlineField]
        public bool inHoldSession;

        [OnlineField]
        public byte currentSegment;

        [OnlineField]
        public int segmentTimer;

        [OnlineField]
        public int spinElapsed;

        [OnlineField]
        public int moveIndex;

        [OnlineField]
        public byte activePose;

        [OnlineField]
        public byte startPose;

        [OnlineField]
        public sbyte startFlipDirection;

        [OnlineFieldHalf]
        public float startHoldAngleDeg;

        [OnlineFieldHalf]
        public float amplitudeScale;

        [OnlineFieldHalf]
        public float speedScale;

        [OnlineField]
        public int startSpearHandMask;

        [OnlineField]
        public int activeSpinFrames;

        [OnlineField]
        public int activeRaiseFrames;

        [OnlineFieldHalf]
        public float activeSpinRotations;

        [OnlineField]
        public int scaledRaiseFrames;

        [OnlineField]
        public int scaledChainRaiseFrames;

        [OnlineField]
        public int scaledLowerFrames;

        [OnlineField]
        public byte autoTriggerKind;

        [OnlineField]
        public int poseTransitionRemaining;

        public State()
        {
        }

        public State(OnlineEntity entity)
        {
            if (entity is not OnlineCreature oc || !entity.isMine)
            {
                return;
            }

            if (oc.realizedCreature is not Player player || player.isNPC)
            {
                return;
            }

            CopyFrom(PlayerTwirlState.Get(player), this);
        }

        public override Type GetDataType() => typeof(TwirlNetworkData);

        public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
        {
            if (onlineEntity.isMine)
            {
                return;
            }

            if (onlineEntity is not OnlineCreature oc || oc.realizedCreature is not Player player || player.isNPC)
            {
                return;
            }

            TwirlNetworkMirror.Apply(player, this);
        }

        internal static void CopyFrom(PlayerTwirlState.Data source, State target)
        {
            target.isTwirling = source.IsTwirling;
            target.inHoldSession = source.InHoldSession;
            target.currentSegment = (byte)source.CurrentSegment;
            target.segmentTimer = source.SegmentTimer;
            target.spinElapsed = source.SpinElapsed;
            target.moveIndex = source.MoveIndex;
            target.activePose = (byte)source.ActivePose;
            target.startPose = (byte)source.StartPose;
            target.startFlipDirection = (sbyte)source.StartFlipDirection;
            target.startHoldAngleDeg = source.StartHoldAngleDeg;
            target.amplitudeScale = source.AmplitudeScale;
            target.speedScale = source.SpeedScale;
            target.startSpearHandMask = source.StartSpearHandMask;
            target.activeSpinFrames = source.ActiveSpinFrames;
            target.activeRaiseFrames = source.ActiveRaiseFrames;
            target.activeSpinRotations = source.ActiveSpinRotations;
            target.scaledRaiseFrames = source.ScaledRaiseFrames;
            target.scaledChainRaiseFrames = source.ScaledChainRaiseFrames;
            target.scaledLowerFrames = source.ScaledLowerFrames;
            target.autoTriggerKind = (byte)source.AutoTriggerKind;
            target.poseTransitionRemaining = source.PoseTransitionRemaining;
        }
    }
}
