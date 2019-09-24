using System;

namespace NeuroSonic.GamePlay.Scoring
{
    public enum GaugeType
    {
        /// <summary>
        /// Starts at 0%, goal is 70%+.
        /// </summary>
        Normal,

        /// <summary>
        /// Starts at 100%, goal is to always stay above 0%.
        /// </summary>
        Hard,
    }
}
