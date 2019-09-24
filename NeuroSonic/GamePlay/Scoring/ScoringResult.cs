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

        public (JudgeResult Kind, int Count)[] Judgements;

        public (int Played, int Total) BtTicks;
        public (int Played, int Total) FxTicks;
        public (int Played, int Total) VolTicks;
    }
}
