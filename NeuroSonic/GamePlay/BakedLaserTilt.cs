using System;
using System.Collections.Generic;

using theori;
using theori.Charting;

using NeuroSonic.Charting;

namespace NeuroSonic.GamePlay
{
    public sealed class BakedLaserTilt
    {
        private class Point : ILinkable<Point>, IComparable<Point>
        {
            public time_t Position;

            public float Alpha;
            public CurveShape Shape;

            public Point Previous { get; set; }
            public Point Next { get; set; }

            public Point(time_t pos, float alpha, CurveShape shape)
            {
                Position = pos;
                Alpha = alpha;
                Shape = shape;
            }

            int IComparable<Point>.CompareTo(Point other) => Position.CompareTo(other.Position);
        }

        public Chart Chart { get; }

        public int StreamIndex { get; }

        private readonly Chart.ChartLane m_objects;
        private readonly OrderedLinkedList<Point> m_points = new OrderedLinkedList<Point>();

        public BakedLaserTilt(Chart chart, int stream)
        {
            Chart = chart;
            StreamIndex = stream;

            m_objects = chart[stream];
        }

        public void Bake()
        {
            // 1 beat transition to anticipation, 1 beat hold anticipation
            // half measure reset

            AnalogEntity current = m_objects.First as AnalogEntity;
            while (current != null)
            {
                bool isTail = current.NextConnected == null;

                bool isSlam = current.IsInstant;
                bool prevWasSlam = current.PreviousConnected?.IsInstant ?? false;

                if (isSlam)
                {
                    float magnitude = MathL.Abs(current.FinalValue - current.InitialValue);
                    time_t maxDistToNext = 0;

                    m_points.Add(new Point(current.AbsolutePosition, current.InitialValue, current.Shape));
                    m_points.Add(new Point(current.AbsolutePosition, current.FinalValue, current.Shape));
                }
                else
                {
                    if (!prevWasSlam)
                        m_points.Add(new Point(current.AbsolutePosition, current.InitialValue, current.Shape));

                    if (isTail)
                        m_points.Add(new Point(current.AbsoluteEndPosition, current.FinalValue, current.Shape));
                }

                current = current.Next as AnalogEntity;
            }
        }

        public float Sample(time_t position)
        {
            foreach (var point in m_points)
            {
                if (point.Position <= position)
                {
                    var next = point.Next;

                    float a = 0.5f, b = 0.5f;
                    if (next != null) // TODO(local): curve shape values from objects
                    {
                        return point.Alpha + (next.Alpha - point.Alpha)
                             * point.Shape.Sample((float)((position - point.Position) / (next.Position - point.Position)), a, b);
                    }
                    else return point.Alpha;
                }
            }

            if (m_points.Count == 0)
                return 0;
            else return m_points[0].Alpha;
        }
    }
}
