using System;
using System.IO;

using theori.Charting;
using theori.Charting.Serialization;

using NeuroSonic.Charting.Conversions;

namespace NeuroSonic.Charting.KShootMania
{
    public class KshChartSerializer : IChartSerializer
    {
        public string ParentDirectory { get; }

        public KshChartSerializer(string parentDirectory)
        {
            ParentDirectory = parentDirectory;
        }

        public Chart LoadFromFile(ChartInfo chartInfo)
        {
            string fileName = Path.Combine(ParentDirectory, chartInfo.Set.FilePath, chartInfo.FileName);

            var ksh = KshChart.CreateFromFile(fileName);
            var chart = ksh.ToVoltex(chartInfo);

            return chart;
        }

        public void SaveToFile(Chart chart)
        {
            throw new NotImplementedException();
        }
    }
}
