using theori;

namespace NeuroSonic.GamePlay.Scoring
{
    public enum JudgeKind
    {
        Miss,

        Bad,
        Near,
        Critical,
        Perfect,

        Passive,
    }

    public struct JudgeResult
    {
        public time_t Difference;
        public JudgeKind Kind;

        public JudgeResult(time_t diff, JudgeKind kind)
        {
            Difference = diff;
            Kind = kind;
        }
    }
}
