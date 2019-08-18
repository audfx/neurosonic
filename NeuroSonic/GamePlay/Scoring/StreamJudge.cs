using System;

using theori;
using theori.Charting;

namespace NeuroSonic.GamePlay.Scoring
{
    public abstract class StreamJudge
    {
        public Chart Chart { get; }
        public LaneLabel Label { get; }
        public Chart.ChartLane Objects => Chart.GetLane(Label);

        protected time_t CurrentPosition { get; private set; }

        /// <summary>
        /// This is the "Input Offset" of the system.
        /// 
        /// A positive value will require the player to hit EARLIER.
        /// A negative value will then require the player to hit LATER.
        /// </summary>
        public time_t JudgementOffset = 0.0;

        public time_t LargestPositionStep = 0.01;

        public bool IsBeingPlayed { get; protected set; } = true;

        public bool AutoPlay = false;

        protected StreamJudge(Chart chart, LaneLabel label)
        {
            Chart = chart;
            Label = label;
        }

        internal void InternalAdvancePosition(time_t position)
        {
            if (position - CurrentPosition > LargestPositionStep * 1.5)
            {
                time_t lastPos = CurrentPosition;
                for (int i = 0, n = MathL.CeilToInt((double)(position - CurrentPosition) / (double)LargestPositionStep); i < n; i++)
                {
                    time_t nextPos = MathL.Lerp((double)lastPos, (double)position, (double)(i + 1) / n);

                    CurrentPosition = nextPos;
                    AdvancePosition(nextPos);
                }
            }
            else
            {
                CurrentPosition = position;
                AdvancePosition(position);
            }
        }

        protected abstract void AdvancePosition(time_t position);
        public abstract int CalculateNumScorableTicks();
    }
}
