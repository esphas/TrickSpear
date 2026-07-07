using RWCustom;
using UnityEngine;

namespace TrickSpear;

internal sealed class TwirlRotationTrail : CosmeticSprite
{
    internal const int MaxSamples = 20;
    internal const float MinSampleDist = 1.1f;
    internal const float FadeOutFrames = 10f;

    private const float EdgeAlphaMin = 0.1f;
    private const float EdgeAlphaMax = 0.98f;
    private const float EdgeAgePower = 2.85f;
    private const float BodyAlphaMin = 0.04f;
    private const float BodyAlphaMax = 0.24f;
    private const float BodyAgePower = 2.35f;

    private readonly Vector2[] _tips = new Vector2[MaxSamples];
    private readonly Vector2[] _dirs = new Vector2[MaxSamples];
    private readonly Color _baseColor;
    private readonly float _baseWidth;

    private int _head;
    private int _count;
    private bool _acceptingSamples = true;
    private float _fadeLife = 1f;
    private float _intensity = 1f;

    internal TwirlRotationTrail(Color baseColor, float baseWidth)
    {
        _baseColor = baseColor;
        _baseWidth = baseWidth;
    }

    internal bool AcceptingSamples => _acceptingSamples;

    internal void AddSample(Vector2 tip, Vector2 dir)
    {
        if (!_acceptingSamples || dir.sqrMagnitude < 0.001f)
        {
            return;
        }

        dir.Normalize();

        if (_count > 0)
        {
            var last = _tips[(_head - 1 + MaxSamples) % MaxSamples];
            if (Custom.DistLess(last, tip, MinSampleDist))
            {
                return;
            }
        }

        _tips[_head] = tip;
        _dirs[_head] = dir;
        _head = (_head + 1) % MaxSamples;
        if (_count < MaxSamples)
        {
            _count++;
        }
    }

    internal void SetIntensity(float intensity)
    {
        _intensity = Mathf.Clamp(intensity, 0f, 1f);
    }

    internal void BeginFadeOut()
    {
        _acceptingSamples = false;
    }

    public override void Update(bool eu)
    {
        if (_acceptingSamples)
        {
            return;
        }

        _fadeLife -= 1f / FadeOutFrames;
        if (_fadeLife <= 0f)
        {
            Destroy();
        }
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        var maxSegments = MaxSamples - 1;
        var tris = new TriangleMesh.Triangle[maxSegments * 2];

        for (var i = 0; i < maxSegments; i++)
        {
            var v = i * 4;
            tris[i * 2] = new TriangleMesh.Triangle(v, v + 1, v + 2);
            tris[i * 2 + 1] = new TriangleMesh.Triangle(v + 1, v + 3, v + 2);
        }

        sLeaser.sprites = new FSprite[1];
        sLeaser.sprites[0] = new TriangleMesh("Futile_White", tris, customColor: true);
        AddToContainer(sLeaser, rCam, null);
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        var mesh = sLeaser.sprites[0] as TriangleMesh;
        if (mesh == null)
        {
            return;
        }

        var maxSegments = MaxSamples - 1;
        var fade = (_acceptingSamples ? _intensity : _intensity * Mathf.Pow(Mathf.Clamp01(_fadeLife), 0.85f));
        var transparent = Custom.RGB2RGBA(_baseColor, 0f);

        if (_count < 2)
        {
            for (var i = 0; i < mesh.verticeColors.Length; i++)
            {
                mesh.verticeColors[i] = transparent;
            }

            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            return;
        }

        var segments = _count - 1;

        for (var seg = 0; seg < maxSegments; seg++)
        {
            if (seg >= segments)
            {
                HideSegment(mesh, seg, transparent);
                continue;
            }

            var tipA = GetTip(seg);
            var tipB = GetTip(seg + 1);
            var dirA = GetDir(seg);
            var dirB = GetDir(seg + 1);
            var tA = segments <= 1 ? 1f : seg / (float)(segments - 1);
            var tB = segments <= 1 ? 1f : (seg + 1) / (float)(segments - 1);

            var tipOuterA = tipA + dirA * TwirlSpearMetrics.BladeForwardReach;
            var tipOuterB = tipB + dirB * TwirlSpearMetrics.BladeForwardReach;
            var shaftA = tipA - dirA * TwirlSpearMetrics.BladeShaftReach;
            var shaftB = tipB - dirB * TwirlSpearMetrics.BladeShaftReach;

            var moveDir = Custom.DirVec(tipA, tipB);
            if (moveDir.sqrMagnitude < 0.001f)
            {
                moveDir = dirB;
            }

            var perp = Custom.PerpendicularVector(moveDir).normalized;
            var outerWa = _baseWidth * Mathf.Lerp(0.4f, 1f, tA);
            var outerWb = _baseWidth * Mathf.Lerp(0.4f, 1f, tB);
            var innerWa = outerWa * 0.3f;
            var innerWb = outerWb * 0.3f;

            var edgeAlphaA = SampleTrailAlpha(tA, EdgeAlphaMin, EdgeAlphaMax, EdgeAgePower);
            var edgeAlphaB = SampleTrailAlpha(tB, EdgeAlphaMin, EdgeAlphaMax, EdgeAgePower);
            var bodyAlphaA = SampleTrailAlpha(tA, BodyAlphaMin, BodyAlphaMax, BodyAgePower);
            var bodyAlphaB = SampleTrailAlpha(tB, BodyAlphaMin, BodyAlphaMax, BodyAgePower);

            edgeAlphaA *= _baseColor.a * fade;
            edgeAlphaB *= _baseColor.a * fade;
            bodyAlphaA *= _baseColor.a * fade;
            bodyAlphaB *= _baseColor.a * fade;

            var v = seg * 4;
            mesh.MoveVertice(v, tipOuterA + perp * outerWa - camPos);
            mesh.MoveVertice(v + 1, tipOuterB + perp * outerWb - camPos);
            mesh.MoveVertice(v + 2, shaftA - perp * innerWa - camPos);
            mesh.MoveVertice(v + 3, shaftB - perp * innerWb - camPos);

            mesh.verticeColors[v] = Custom.RGB2RGBA(_baseColor, edgeAlphaA);
            mesh.verticeColors[v + 1] = Custom.RGB2RGBA(_baseColor, edgeAlphaB);
            mesh.verticeColors[v + 2] = Custom.RGB2RGBA(_baseColor, bodyAlphaA);
            mesh.verticeColors[v + 3] = Custom.RGB2RGBA(_baseColor, bodyAlphaB);
        }

        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
    {
        newContatiner ??= rCam.ReturnFContainer("Foreground");
        base.AddToContainer(sLeaser, rCam, newContatiner);
    }

    private static void HideSegment(TriangleMesh mesh, int seg, Color transparent)
    {
        for (var i = 0; i < 4; i++)
        {
            mesh.verticeColors[seg * 4 + i] = transparent;
        }
    }

    private static float SampleTrailAlpha(float age, float min, float max, float power) =>
        Mathf.Lerp(min, max, Mathf.Pow(Mathf.Clamp01(age), power));

    private Vector2 GetTip(int index)
    {
        var start = (_head - _count + MaxSamples) % MaxSamples;
        return _tips[(start + index) % MaxSamples];
    }

    private Vector2 GetDir(int index)
    {
        var start = (_head - _count + MaxSamples) % MaxSamples;
        return _dirs[(start + index) % MaxSamples];
    }
}
