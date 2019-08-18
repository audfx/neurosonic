using System;
using System.Collections.Generic;

using theori;
using theori.Charting;

using NeuroSonic.Charting;

namespace NeuroSonic.GamePlay
{
    sealed class LaserRollSampler
    {
        sealed class Segment
        {
            public time_t StartTime, EndTime;
            public float StartValue, EndValue;

            public void Sample(time_t time) => MathL.Lerp(StartValue, EndValue, (float)((double)(time - StartTime) / (double)(EndTime - StartTime)));
        }

        private Chart m_chart;
        private List<Segment> m_segments = new List<Segment>();

        public LaserRollSampler(Chart chart)
        {
            m_chart = chart;

            Generate();
        }

        private void Generate()
        {
            AnalogEntity left, right;
            LaserApplicationEvent appl;
            LaserParamsEvent pars;
            
            left = m_chart[6].First as AnalogEntity;
            right = m_chart[7].First as AnalogEntity;
            appl = m_chart[NscLane.LaserEvent].First as LaserApplicationEvent;
            pars = m_chart[NscLane.LaserEvent].First as LaserParamsEvent;

            while (left != null || right != null)
            {
                void HandleLoneSegment(ref AnalogEntity lone, float dir)
                {
                    m_segments.Add(new Segment()
                    {
                        StartTime = lone.AbsolutePosition,
                        EndTime = lone.AbsoluteEndPosition,
                        StartValue = lone.InitialValue * dir,
                        EndValue = lone.FinalValue * dir,
                    });
                    lone = lone.Next as AnalogEntity;
                }

                void HandleOverlap(ref AnalogEntity pri, ref AnalogEntity sec, float dir)
                {
                    m_segments.Add(new Segment()
                    {
                        StartTime = pri.AbsolutePosition,
                        EndTime = sec.AbsolutePosition,
                        StartValue = pri.InitialValue * dir,
                        EndValue = -dir * sec.InitialValue + MathL.Lerp(pri.InitialValue, pri.FinalValue, (float)((double)(sec.AbsolutePosition - pri.AbsolutePosition) / (double)pri.AbsoluteDuration)),
                    });
                    sec = sec.Next as AnalogEntity;
                }
                
                if (left == null || right == null)
                {
                    if (left != null)
                        HandleLoneSegment(ref left, 1);
                    else HandleLoneSegment(ref right, -1);
                }
                else // both non-null
                {
                    if (left.Position >= right.EndPosition)
                        HandleLoneSegment(ref right, -1);
                    else if (right.Position >= left.EndPosition)
                        HandleLoneSegment(ref left, 1);
                    else
                    {
                        // overlap
                        if (left.EndPosition > right.EndPosition)
                            HandleOverlap(ref left, ref right, 1);
                        else HandleOverlap(ref right, ref left, -1);
                    }
                }
            }
        }

        public float Sample(time_t time)
        {
            return 0;
        }
    }
}
