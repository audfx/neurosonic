using theori.Scoring;

namespace NeuroSonic.GamePlay.Scoring
{
    public struct ScoringResult
    {
        public int Score;
        public int MaxCombo;
        public ScoreRank Rank;

        public int LastHiScore;
        public ScoreRank LastHiRank;

        public double Gauge;
        public double[] GaugeSamples;
        public GaugeType GaugeType;

        public int TotalBtCount;
        public int TotalFxCount;
        public int TotalVolCount;

        public int PassiveBtCount;
        public int PerfectBtCount;
        public int CriticalBtCount;
        public int EarlyBtCount;
        public int LateBtCount;
        public int BadBtCount;

        public int PassiveFxCount;
        public int PerfectFxCount;
        public int CriticalFxCount;
        public int EarlyFxCount;
        public int LateFxCount;
        public int BadFxCount;

        public int PassiveVolCount;
     
        public int MissCount;
    }
}
