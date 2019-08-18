using System;

using theori.Charting;
using theori.Charting.Serialization;

using NeuroSonic.Charting;

namespace NeuroSonic.Editor
{
    public class ChartEditorLayer : NscLayer
    {
        private ChartInfo m_chartInfo;

        private Chart m_chart;

        public ChartEditorLayer(ChartInfo chartInfo)
        {
            m_chartInfo = chartInfo;
        }

        public override bool AsyncLoad()
        {
            return true;
        }

        public override bool AsyncFinalize()
        {
            return true;
        }

        public override void Init()
        {
        }
    }
}
