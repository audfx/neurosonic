using System;
using NeuroSonic.Charting;
using theori;
using theori.Charting;

namespace NeuroSonic.GamePlay.Scoring
{
    public sealed class MasterJudge
    {
        public Chart Chart { get; }

        private time_t m_position = double.MinValue;
        public time_t Position
        {
            get => m_position;
            set
            {
                if (value.Seconds.ApproxEq(m_position.Seconds, 0.001)) return;

                if (value < m_position)
                    throw new System.Exception("Cannot rewind score judgement");
                else if (value == m_position) return;

                m_position = value;
                AdvancePosition(value);
            }
        }

        public int Score => (int)(m_tickValue * 10_000_000L / m_maxTickValue);
        public double Accuracy => m_expectedTickValue == 0 ? 1.0 : (double)m_tickValue / m_expectedTickValue;

        private int m_maxTickValue, m_tickValue, m_expectedTickValue;
        private int m_maxTickWorth = 2;

        public double Gauge { get; private set; } = 0.0;
        private double m_activeGaugeGain, m_passiveGaugeGain;

        private readonly StreamJudge[] m_judges = new StreamJudge[8];

        public StreamJudge this[int index] => m_judges[index];

        public MasterJudge(Chart chart)
        {
            Chart = chart;

            int numChipTicks = 0, numHoldTicks = 0;
            for (int i = 0; i < 6; i++)
            {
                var judge = new ButtonJudge(chart, i);
                judge.OnTickProcessed += ButtonJudge_OnTickProcessed;

                m_maxTickValue += judge.CalculateNumScorableTicks();
                m_judges[i] = judge;

                int[] tickKinds = judge.GetCategorizedTicks();
                numChipTicks += tickKinds[0];
                numHoldTicks += tickKinds[1];
            }

            int numLaserTicks = 0;
            for (int i = 0; i < 2; i++)
            {
                var judge = new LaserJudge(chart, i + 6);
                judge.OnTickProcessed += ButtonJudge_OnTickProcessed;

                m_maxTickValue += judge.CalculateNumScorableTicks();
                m_judges[i + 6] = judge;

                int[] tickKinds = judge.GetCategorizedTicks();
                numLaserTicks += tickKinds[0];
            }

            int numButtonTicks = numChipTicks + numHoldTicks;

            int numActiveTicks = numChipTicks;
            int numPassiveTicks = numHoldTicks + numLaserTicks;

            int totalNumTicks = numButtonTicks + numLaserTicks;

            double chartPlayableDuration = (double)(chart.TimeEnd - chart.TimeStart) / 60.0;
            double defaultMaxGauge = 2 + MathL.Log(0.1 + 0.6 * chartPlayableDuration);

            double maxGauge = defaultMaxGauge;
            Logger.Log($"Maximum Guage: {maxGauge * 100}%");

            if (numActiveTicks == 0 && numPassiveTicks != 0)
                m_passiveGaugeGain = maxGauge / numPassiveTicks;
            else if (numActiveTicks != 0 && numPassiveTicks == 0)
                m_activeGaugeGain = maxGauge / numActiveTicks;
            else
            {
                m_activeGaugeGain = (maxGauge * 20) / (5 * (numPassiveTicks + 4 * numActiveTicks));
                m_passiveGaugeGain = m_activeGaugeGain / 4;
            }

            m_maxTickValue *= m_maxTickWorth;
        }

        private void ButtonJudge_OnTickProcessed(Entity obj, time_t when, JudgeResult result)
        {
            m_expectedTickValue += m_maxTickWorth;
            switch (result.Kind)
            {
                case JudgeKind.Passive:
                case JudgeKind.Critical:
                case JudgeKind.Perfect:
                {
                    m_tickValue += 2;
                } break;

                case JudgeKind.Near:
                {
                    m_tickValue += 1;
                } break;
            }

            switch (result.Kind)
            {
                case JudgeKind.Passive:
                case JudgeKind.Critical:
                case JudgeKind.Perfect:
                case JudgeKind.Near:
                {
                    Gauge += (obj is ButtonEntity button ? (button.IsInstant ? m_activeGaugeGain : m_passiveGaugeGain) : m_passiveGaugeGain);
                } break;

                case JudgeKind.Bad:
                case JudgeKind.Miss:
                {
                    double activeDrain = 0.02, passiveDrain = activeDrain / 4;
                    Gauge -= (obj is ButtonEntity button ? (button.IsInstant ? activeDrain : passiveDrain) : passiveDrain);
                } break;
            }

            Gauge = MathL.Clamp01(Gauge);
        }

        private void AdvancePosition(time_t position)
        {
            for (int i = 0; i < 8; i++)
            {
                var judge = m_judges[i];
                if (judge != null)
                    judge.InternalAdvancePosition(position);
            }
        }
    }
}
