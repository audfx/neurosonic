using System;
using NeuroSonic.Charting;
using theori;
using theori.Charting;
using theori.Scoring;

namespace NeuroSonic.GamePlay.Scoring
{
    public sealed class MasterJudge
    {
        public Chart Chart { get; }
        public GaugeType GaugeType { get; }

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

        internal int m_maxBtCount, m_maxFxCount, m_maxVolCount;
        internal int m_passiveBtCount, m_perfectBtCount, m_criticalBtCount, m_earlyBtCount, m_lateBtCount, m_badBtCount;
        internal int m_passiveFxCount, m_perfectFxCount, m_criticalFxCount, m_earlyFxCount, m_lateFxCount, m_badFxCount;
        internal int m_passiveVolCount, m_missCount;

        public double Gauge { get; private set; } = 0.0;
        private double m_activeGaugeGain, m_passiveGaugeGain;

        private readonly StreamJudge[] m_judges = new StreamJudge[8];

        public StreamJudge this[int index] => m_judges[index];

        public MasterJudge(Chart chart, GaugeType gaugeType)
        {
            Chart = chart;
            GaugeType = gaugeType;

            int numChipTicks = 0, numHoldTicks = 0;
            for (int i = 0; i < 6; i++)
            {
                var judge = new ButtonJudge(chart, i);
                judge.OnTickProcessed += ButtonJudge_OnTickProcessed;

                int ticks = judge.CalculateNumScorableTicks();
                if (i < 4)
                    m_maxBtCount += ticks / m_maxTickWorth;
                else m_maxFxCount += ticks / m_maxTickWorth;
                m_maxTickValue += ticks;
                m_judges[i] = judge;

                int[] tickKinds = judge.GetCategorizedTicks();
                numChipTicks += tickKinds[0];
                numHoldTicks += tickKinds[1];
            }

            int numLaserTicks = 0;
            for (int i = 0; i < 2; i++)
            {
                var judge = new LaserJudge(chart, i + 6);
                judge.OnTickProcessed += (e, when, result) => ButtonJudge_OnTickProcessed(e, when, result, false);

                int ticks = judge.CalculateNumScorableTicks(); ;
                m_maxVolCount += ticks / m_maxTickWorth;
                m_maxTickValue += ticks;
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

        /// <summary>
        /// The scoring result is incomplete, but contains all values the judge knows about.
        /// </summary>
        public ScoringResult GetScoringResult()
        {
            var rank = ScoreRank.F;
            if (Score == 10_000_000) rank = ScoreRank.Perfect;
            else if (Score >= 9_900_000) rank = ScoreRank.S;
            else if (Score >= 9_800_000) rank = ScoreRank.AAAX;
            else if (Score >= 9_700_000) rank = ScoreRank.AAA;
            else if (Score >= 9_500_000) rank = ScoreRank.AAX;
            else if (Score >= 9_300_000) rank = ScoreRank.AA;
            else if (Score >= 9_000_000) rank = ScoreRank.AX;
            else if (Score >= 8_700_000) rank = ScoreRank.A;
            else if (Score >= 8_000_000) rank = ScoreRank.B;
            else if (Score >= 7_000_000) rank = ScoreRank.C;

            return new ScoringResult()
            {
                Score = Score,
                Rank = rank,

                Gauge = Gauge,
                GaugeType = GaugeType,

                TotalBtCount = m_maxBtCount,
                TotalFxCount = m_maxFxCount,
                TotalVolCount = m_maxVolCount,

                PassiveBtCount = m_passiveBtCount,
                PerfectBtCount = m_perfectBtCount,
                CriticalBtCount = m_criticalBtCount,
                EarlyBtCount = m_earlyBtCount,
                LateBtCount = m_lateBtCount,
                BadBtCount = m_badBtCount,

                PassiveFxCount = m_passiveFxCount,
                PerfectFxCount = m_perfectFxCount,
                CriticalFxCount = m_criticalFxCount,
                EarlyFxCount = m_earlyFxCount,
                LateFxCount = m_lateFxCount,
                BadFxCount = m_badFxCount,

                PassiveVolCount = m_passiveVolCount,

                MissCount = m_missCount,
            };
        }

        private void ButtonJudge_OnTickProcessed(Entity e, time_t when, JudgeResult result, bool isEarly)
        {
            m_expectedTickValue += m_maxTickWorth;
            switch (result.Kind)
            {
                case JudgeKind.Passive:
                case JudgeKind.Perfect:
                case JudgeKind.Critical: m_tickValue += 2; break;
                case JudgeKind.Near: m_tickValue += 1; break;
            }

            if (result.Kind == JudgeKind.Miss)
                m_missCount++;
            else
            {
                if (e is ButtonEntity)
                {
                    bool isFx = e.Lane == 4 || e.Lane == 5;
                    switch (result.Kind)
                    {
                        case JudgeKind.Passive: if (isFx) m_passiveFxCount++; else m_passiveBtCount++; break;
                        case JudgeKind.Perfect: if (isFx) m_perfectFxCount++; else m_perfectBtCount++; break;
                        case JudgeKind.Critical: if (isFx) m_criticalFxCount++; else m_criticalBtCount++; break;
                        case JudgeKind.Near:
                        {
                            if (isEarly)
                            {
                                if (isFx) m_earlyFxCount++; else m_earlyBtCount++;
                            }
                            else
                            {
                                if (isFx) m_lateFxCount++; else m_lateBtCount++;
                            }
                        }
                        break;
                        case JudgeKind.Bad: if (isFx) m_badFxCount++; else m_badBtCount++; break;
                    }
                }
                else
                {
                    switch (result.Kind)
                    {
                        case JudgeKind.Passive: m_passiveVolCount++; break;
                    }
                }
            }

            switch (result.Kind)
            {
                case JudgeKind.Passive:
                case JudgeKind.Perfect:
                case JudgeKind.Critical:
                case JudgeKind.Near:
                {
                    Gauge += (e is ButtonEntity button ? (button.IsInstant ? m_activeGaugeGain : m_passiveGaugeGain) : m_passiveGaugeGain);
                } break;

                case JudgeKind.Bad:
                case JudgeKind.Miss:
                {
                    double activeDrain = 0.02, passiveDrain = activeDrain / 4;
                    Gauge -= (e is ButtonEntity button ? (button.IsInstant ? activeDrain : passiveDrain) : passiveDrain);
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
