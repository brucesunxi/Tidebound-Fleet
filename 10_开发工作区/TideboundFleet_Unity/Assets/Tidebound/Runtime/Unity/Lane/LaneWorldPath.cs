using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tidebound.Unity.Lane
{
    /// <summary>A presentation-only Catmull-Rom path. Logical transit duration is owned by TransitSystem.</summary>
    public sealed class LaneWorldPath
    {
        private readonly Vector3[] points;

        public Vector3 Start => points[0];
        public Vector3 End => points[points.Length - 1];
        public IReadOnlyList<Vector3> ControlPoints => Array.AsReadOnly(points);

        public LaneWorldPath(params Vector3[] controlPoints)
        {
            if (controlPoints == null) throw new ArgumentNullException(nameof(controlPoints));
            if (controlPoints.Length < 2)
                throw new ArgumentException("A lane path requires at least two points.", nameof(controlPoints));
            points = (Vector3[])controlPoints.Clone();
        }

        public Vector3 Sample(float progress)
        {
            var t = Mathf.Clamp01(progress);
            if (t <= 0f) return Start;
            if (t >= 1f) return End;
            if (points.Length == 2) return Vector3.LerpUnclamped(points[0], points[1], t);

            var scaled = t * (points.Length - 1);
            var segment = Mathf.Min(Mathf.FloorToInt(scaled), points.Length - 2);
            var local = scaled - segment;
            var p0 = points[Mathf.Max(0, segment - 1)];
            var p1 = points[segment];
            var p2 = points[segment + 1];
            var p3 = points[Mathf.Min(points.Length - 1, segment + 2)];
            return CatmullRom(p0, p1, p2, p3, local);
        }

        public Vector3 Tangent(float progress)
        {
            const float sampleOffset = 0.001f;
            var from = Sample(Mathf.Max(0f, progress - sampleOffset));
            var to = Sample(Mathf.Min(1f, progress + sampleOffset));
            var tangent = to - from;
            return tangent.sqrMagnitude > 0.000001f ? tangent.normalized : Vector3.forward;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return 0.5f * ((2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
