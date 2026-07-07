using RWCustom;
using UnityEngine;

namespace TrickSpear;

internal static class TwirlMoves
{
    internal enum Kind
    {
        Chest,
        Back,
        Flower,
        AirClose,
        AirWrap,
        Overhead,
        OverheadSweep,
        SlideLow,
        SlideSweep,
        BellySkim,
        BellySweep,
    }

    internal readonly struct Definition
    {
        internal readonly Kind MoveKind;
        internal readonly Vector2 RaiseDelta;
        internal readonly float Rotations;
        internal readonly int SpinFrames;
        internal readonly float WristRadiusX;
        internal readonly float WristRadiusY;
        internal readonly float WristHarmonics;

        internal Definition(
            Kind moveKind,
            Vector2 raiseDelta,
            float rotations,
            int spinFrames,
            float wristRadiusX,
            float wristRadiusY,
            float wristHarmonics)
        {
            MoveKind = moveKind;
            RaiseDelta = raiseDelta;
            Rotations = rotations;
            SpinFrames = spinFrames;
            WristRadiusX = wristRadiusX;
            WristRadiusY = wristRadiusY;
            WristHarmonics = wristHarmonics;
        }
    }

    private static readonly Definition[] StandSequence =
    {
        new(Kind.Chest, new Vector2(6f, 12f), 2.25f, 28, 3f, 2f, 2f),
        new(Kind.Back, new Vector2(0f, 6f), 1.75f, 24, 4f, 3.5f, 2.5f),
        new(Kind.Flower, new Vector2(7f, 7f), 2.5f, 26, 5f, 4f, 4f),
    };

    private static readonly Definition[] AirborneSequence =
    {
        new(Kind.AirClose, new Vector2(-2f, 5f), 2.75f, 24, 5f, 6f, 3f),
        new(Kind.AirWrap, new Vector2(0f, 3f), 2.5f, 22, 6f, 7f, 2f),
    };

    private static readonly Definition[] CrawlSequence =
    {
        new(Kind.Overhead, new Vector2(0f, 28f), 2f, 24, 2f, 1.5f, 1f),
        new(Kind.OverheadSweep, new Vector2(0f, 32f), 2.25f, 22, 3f, 2f, 1.5f),
    };

    private static readonly Definition[] SlideSequence =
    {
        new(Kind.SlideLow, new Vector2(4f, 4f), 2f, 22, 4f, 2.5f, 2f),
        new(Kind.SlideSweep, new Vector2(6f, 6f), 2.25f, 24, 5f, 3f, 2.5f),
    };

    private static readonly Definition[] BellySlideSequence =
    {
        new(Kind.BellySkim, new Vector2(2f, 2f), 2f, 20, 3f, 1.5f, 2f),
        new(Kind.BellySweep, new Vector2(4f, 3f), 2.25f, 22, 4f, 2f, 2.5f),
    };

    internal static Definition Get(TwirlPose pose, int index)
    {
        var seq = GetSequence(pose);
        return seq[index % seq.Length];
    }

    internal static int SequenceCount(TwirlPose pose) => GetSequence(pose).Length;

    internal static Vector2 DefaultGraspRelative(TwirlPose pose, int handIndex)
    {
        var side = handIndex == 0 ? -1f : 1f;
        return pose switch
        {
            TwirlPose.Stand => new Vector2(-20f + 40f * handIndex, -12f),
            TwirlPose.Airborne => new Vector2(side * 8f, -2f),
            TwirlPose.Crawl => new Vector2(side * 14f, 18f),
            TwirlPose.Slide => SlideGraspRelative(handIndex, 1),
            TwirlPose.BellySlide => BellySlideGraspRelative(handIndex, 1),
            _ => new Vector2(-20f + 40f * handIndex, -12f),
        };
    }

    internal static float ResolveStartHoldAngleDeg(Player player, TwirlPose pose)
    {
        var flip = player.flipDirection == 0 ? 1f : player.flipDirection;
        var facing = new Vector2(flip, 0f);

        if (player.input[0].x != 0 || player.input[0].y != 0)
        {
            facing = new Vector2(player.input[0].x, player.input[0].y).normalized;
        }

        return pose switch
        {
            TwirlPose.Stand => Custom.VecToDeg(facing),
            TwirlPose.Airborne => Custom.VecToDeg(facing) + 90f * flip,
            TwirlPose.Crawl => Custom.VecToDeg(facing),
            TwirlPose.Slide => Custom.VecToDeg(facing),
            TwirlPose.BellySlide => Custom.VecToDeg(facing),
            _ => Custom.VecToDeg(facing),
        };
    }

    internal static Vector2 ComputeRaisedRelative(
        TwirlPose pose,
        Vector2 rest,
        int handIndex,
        Definition move,
        float amplitudeScale,
        int flipDirection = 1)
    {
        if (pose == TwirlPose.Crawl)
        {
            return CrawlRaisedRelative(handIndex, flipDirection, move, amplitudeScale);
        }

        if (pose == TwirlPose.Slide)
        {
            return SlideRaisedRelative(handIndex, flipDirection, move, amplitudeScale);
        }

        if (pose == TwirlPose.BellySlide)
        {
            return BellySlideRaisedRelative(handIndex, flipDirection, move, amplitudeScale);
        }

        var side = handIndex == 0 ? -1f : 1f;

        switch (move.MoveKind)
        {
            case Kind.Chest:
                return rest + move.RaiseDelta * amplitudeScale;

            case Kind.Back:
                return rest + move.RaiseDelta * amplitudeScale
                    + new Vector2(side * 16f, -8f) * amplitudeScale;

            case Kind.Flower:
                return rest + move.RaiseDelta * amplitudeScale;

            case Kind.AirClose:
            {
                var center = new Vector2(side * 2f, 1f);
                return Vector2.Lerp(rest, center, 0.72f * amplitudeScale);
            }

            case Kind.AirWrap:
            {
                var center = new Vector2(0f, 0f);
                var raised = Vector2.Lerp(rest, center, 0.85f * amplitudeScale);
                return raised + new Vector2(side * 3f, 3f) * amplitudeScale;
            }

            case Kind.Overhead:
                return CrawlRaisedRelative(handIndex, flipDirection, move, amplitudeScale);

            case Kind.OverheadSweep:
                return CrawlRaisedRelative(handIndex, flipDirection, move, amplitudeScale)
                    + new Vector2((handIndex == 0 ? -1f : 1f) * 4f, 4f) * amplitudeScale;

            case Kind.SlideLow:
                return SlideRaisedRelative(handIndex, flipDirection, move, amplitudeScale);

            case Kind.SlideSweep:
                return SlideRaisedRelative(handIndex, flipDirection, move, amplitudeScale)
                    + new Vector2((handIndex == 0 ? -1f : 1f) * 3f, 2f) * amplitudeScale;

            case Kind.BellySkim:
                return BellySlideRaisedRelative(handIndex, flipDirection, move, amplitudeScale);

            case Kind.BellySweep:
                return BellySlideRaisedRelative(handIndex, flipDirection, move, amplitudeScale)
                    + new Vector2((handIndex == 0 ? -1f : 1f) * 2f, 1f) * amplitudeScale;

            default:
                return rest + move.RaiseDelta * amplitudeScale;
        }
    }

    internal static Vector2 CrawlGraspRelative(int handIndex, int flipDirection)
    {
        var facing = flipDirection >= 0 ? 1f : -1f;
        var side = handIndex == 0 ? -1f : 1f;
        return new Vector2(facing * 10f + side * 3f, 20f);
    }

    internal static Vector2 CrawlRaisedRelative(int handIndex, int flipDirection, Definition move, float amplitudeScale)
    {
        var facing = flipDirection >= 0 ? 1f : -1f;
        var side = handIndex == 0 ? -1f : 1f;
        var height = move.MoveKind == Kind.OverheadSweep ? 56f : 50f;
        return new Vector2(facing * 16f + side * 5f, height * amplitudeScale);
    }

    internal static Vector2 SlideGraspRelative(int handIndex, int flipDirection)
    {
        var facing = flipDirection >= 0 ? 1f : -1f;
        var side = handIndex == 0 ? -1f : 1f;
        return new Vector2(facing * 12f + side * 4f, -4f);
    }

    internal static Vector2 SlideRaisedRelative(int handIndex, int flipDirection, Definition move, float amplitudeScale)
    {
        var facing = flipDirection >= 0 ? 1f : -1f;
        var side = handIndex == 0 ? -1f : 1f;
        var height = move.MoveKind == Kind.SlideSweep ? 14f : 10f;
        return new Vector2(facing * 18f + side * 6f, height * amplitudeScale);
    }

    internal static Vector2 BellySlideGraspRelative(int handIndex, int flipDirection)
    {
        var facing = flipDirection >= 0 ? 1f : -1f;
        var side = handIndex == 0 ? -1f : 1f;
        return new Vector2(facing * 10f + side * 3f, 4f);
    }

    internal static Vector2 BellySlideRaisedRelative(int handIndex, int flipDirection, Definition move, float amplitudeScale)
    {
        var facing = flipDirection >= 0 ? 1f : -1f;
        var side = handIndex == 0 ? -1f : 1f;
        var height = move.MoveKind == Kind.BellySweep ? 12f : 8f;
        return new Vector2(facing * 14f + side * 4f, height * amplitudeScale);
    }

    private static Definition[] GetSequence(TwirlPose pose) =>
        pose switch
        {
            TwirlPose.Stand => StandSequence,
            TwirlPose.Airborne => AirborneSequence,
            TwirlPose.Crawl => CrawlSequence,
            TwirlPose.Slide => SlideSequence,
            TwirlPose.BellySlide => BellySlideSequence,
            _ => StandSequence,
        };
}
