using System.Diagnostics;
using theori.Charting;

namespace NeuroSonic.Charting
{
    public sealed class NeuroSonicChartFactory : ChartFactory
    {
        public static readonly NeuroSonicChartFactory Instance = new NeuroSonicChartFactory();

        public override Chart CreateNew()
        {
            var chart = new Chart(NeuroSonicGameMode.Instance);
            for (int i = 0; i < 6; i++)
                chart.CreateTypedLane<ButtonEntity>(i, EntityRelation.Equal);
            for (int i = 0; i < 2; i++)
                chart.CreateTypedLane<AnalogEntity>(i + 6, EntityRelation.Equal);

            chart.CreateTypedLane<HighwayTypedEvent>(NscLane.HighwayEvent, EntityRelation.Subclass);
            chart.CreateTypedLane<ButtonTypedEvent>(NscLane.ButtonEvent, EntityRelation.Subclass);
            chart.CreateTypedLane<LaserTypedEvent>(NscLane.LaserEvent, EntityRelation.Subclass);

            chart.CreateTypedLane<GraphPointEvent>(NscLane.CameraZoom);
            chart.CreateTypedLane<GraphPointEvent>(NscLane.CameraPitch);
            chart.CreateTypedLane<GraphPointEvent>(NscLane.CameraOffset);
            chart.CreateTypedLane<GraphPointEvent>(NscLane.CameraTilt);

            return chart;
        }
    }
}
