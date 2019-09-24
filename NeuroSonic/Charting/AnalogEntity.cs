using System;

using theori;
using theori.Charting;

namespace NeuroSonic.Charting
{
    public enum CurveShape : byte
    {
        Linear,
        Cosine,
        ThreePoint,
    }

    public static class CurveShapeExt
    {
        public static float Sample(this CurveShape shape, float alpha, float a = 0.0f, float b = 0.0f)
        {
            switch (shape)
            {
                default:
                case CurveShape.Linear: return alpha;

                case CurveShape.Cosine:
                {
                    // linear interpolator, from regular sampling to squished sampling
                    static float r(float x, float a) => a * m(x) + (1 - a) * x;
                    // desmos :: m\left(x\right)=\frac{2\sin^{-1}\left(\sqrt[3]{\left(x-0.5\right)\left(2\sin\left(\frac{\pi}{4}\right)^3\right)}\right)}{\pi}+0.5\left\{0\le x\le1\right\}
                    static float m(float x)
                    {
                        float pi = MathL.Pi;

                        float _2sinpi4to3 = 2 * MathL.Pow(MathL.Sin(pi * 0.25f), 3);
                        float _cbrtv = MathL.Cbrt((x - 0.5f) * _2sinpi4to3);
                        float _asincb = MathL.Asin(_cbrtv);
                        float result = 0.5f + (2 * _asincb / pi);

                        return result;
                    }

                    alpha = MathL.Lerp(alpha, alpha * alpha, b - 0.5f);
                    float angle = r(alpha, a * 0.75f) * MathL.Pi;
                    return (1 - (float)Math.Cos(angle)) * 0.5f;
                }

                case CurveShape.ThreePoint:
                {
                    float t = (a - MathL.Sqrt(a * a + alpha - 2 * a * alpha)) / (-1 + 2 * a);
                    return 2 * (1 - t) * t * b + t * t;
                }
            }
        }
    }

    // TODO(local): Convert analog objects to a sequence of graph points

    [EntityType("Analog")]
    public sealed class AnalogEntity : Entity
    {
        private float m_initialValue, m_finalValue;
        private CurveShape m_shape = CurveShape.Linear;
        private float m_a, m_b;
        private bool m_extended;

        public AnalogEntity Head => FirstConnectedOf<AnalogEntity>();
        public AnalogEntity Tail => LastConnectedOf<AnalogEntity>();

        public int DirectionSign => MathL.Sign(FinalValue - InitialValue);

        public bool IsSlam => IsInstant;

        [TheoriProperty("initial")]
        public float InitialValue
        {
            get => m_initialValue;
            set => SetPropertyField(nameof(InitialValue), ref m_initialValue, value);
        }

        [TheoriProperty("final")]
        public float FinalValue
        {
            get => m_finalValue;
            set => SetPropertyField(nameof(FinalValue), ref m_finalValue, value);
        }

        [TheoriIgnoreDefault]
        [TheoriProperty("shape")]
        public CurveShape Shape
        {
            get => m_shape;
            set => SetPropertyField(nameof(Shape), ref m_shape, value);
        }

        [TheoriIgnoreDefault]
        [TheoriProperty("a")]
        public float CurveA
        {
            get => m_a;
            set => SetPropertyField(nameof(CurveA), ref m_a, MathL.Clamp(value, 0, 1));
        }

        [TheoriIgnoreDefault]
        [TheoriProperty("b")]
        public float CurveB
        {
            get => m_b;
            set => SetPropertyField(nameof(CurveB), ref m_b, MathL.Clamp(value, 0, 1));
        }

        [TheoriIgnoreDefault]
        [TheoriProperty("extended")]
        public bool RangeExtended
        {
            get => m_extended;
            set => SetPropertyField(nameof(RangeExtended), ref m_extended, value);
        }

        public float SampleValue(time_t position)
        {
            if (position <= AbsolutePosition) return InitialValue;
            if (position >= AbsoluteEndPosition) return FinalValue;

            float alpha = Shape.Sample((float)((position - AbsolutePosition).Seconds / AbsoluteDuration.Seconds), CurveA, CurveB);
            return MathL.Lerp(InitialValue, FinalValue, alpha);
        }

        public float SampleValueRelative(float value)
        {
            if (value <= 0) return InitialValue;
            if (value >= 1.0f) return FinalValue;

            float alpha = Shape.Sample(value, CurveA, CurveB);
            return MathL.Lerp(InitialValue, FinalValue, alpha);
        }
    }
}
