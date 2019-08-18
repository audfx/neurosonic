using System;
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

        private readonly StreamJudge[] m_judges = new StreamJudge[8];

        public StreamJudge this[int index] => m_judges[index];

        public MasterJudge(Chart chart)
        {
            Chart = chart;

            for (int i = 0; i < 6; i++)
            {
                var judge = new ButtonJudge(chart, i);
                judge.OnTickProcessed += ButtonJudge_OnTickProcessed;

                m_maxTickValue += judge.CalculateNumScorableTicks();
                m_judges[i] = judge;
            }

            for (int i = 0; i < 2; i++)
            {
                var judge = new LaserJudge(chart, i + 6);
                judge.OnTickProcessed += ButtonJudge_OnTickProcessed;

                m_maxTickValue += judge.CalculateNumScorableTicks();
                m_judges[i + 6] = judge;
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
                    m_tickValue += 2;
                    break;

                case JudgeKind.Near:
                    m_tickValue += 1;
                    break;
            }
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
