using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    public struct Spline
    {
        public const int k_Precision = 128;
        public const float k_Step = 1f / k_Precision;

        bool loop;
        float zeroValue;
        float range;
        public float[] cachedData;
        AnimationCurve internalCurve;

        public Spline(float zeroValue, bool loop, Vector2 bounds)
        {
            this.zeroValue = zeroValue;
            this.loop = loop;
            this.range = bounds.magnitude;
            cachedData = new float[k_Precision];
            internalCurve = new AnimationCurve();
        }

        public void SetCurve(AnimationCurve curve)
        {
            var length = curve.length;

            if (loop && length > 1)
            {
                if (internalCurve == null)
                    internalCurve = new AnimationCurve();

                var prev = curve[length - 1];
                prev.time -= range;
                var next = curve[0];
                next.time += range;
                internalCurve.keys = curve.keys;
                internalCurve.AddKey(prev);
                internalCurve.AddKey(next);
            }

            for (int i = 0; i < k_Precision; i++)
                cachedData[i] = Evaluate(curve, (float)i * k_Step, length);
        }

        public float Evaluate(AnimationCurve curve, float t, int length)
        {
            if (length == 0)
                return zeroValue;

            if (!loop || length == 1)
                return curve.Evaluate(t);

            return internalCurve.Evaluate(t);
        }
    }
}