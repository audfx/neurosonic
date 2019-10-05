using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeuroSonic.Charting;
using NeuroSonic.Charting.Conversions;
using NeuroSonic.Charting.KShootMania;
using NeuroSonic.Platform;
using NeuroSonic.Startup;
using theori.Charting;
using theori.Charting.Serialization;

namespace NeuroSonic.ChartSelect
{
    class ChartImportLayer : BaseMenuLayer
    {
        private readonly string m_title;
        protected override string Title => m_title;

        private readonly string m_rootDirectory;

        private CancellationTokenSource? m_cancellation;
        private Task? m_task;

        public ChartImportLayer(string rootDirectory)
        {
            m_title = $"Chart Importing `{ rootDirectory }`";
            m_rootDirectory = rootDirectory;
        }

        public override void Initialize()
        {
            base.Initialize();

            m_cancellation = new CancellationTokenSource();
            m_task = Task.Run(() => DoWork());
        }

        protected override void GenerateMenuItems()
        {
            AddMenuItem(new MenuItem(NextOffset, "Cancel Operation (may leave unfinished work!)", () =>
            {
                m_cancellation?.Cancel();
                m_cancellation = null;

                Pop();
            }));
        }

        public override void Update(float delta, float total)
        {
            base.Update(delta, total);

            if (m_task == null) return;
            if (m_task.IsCompleted) Pop();
        }

        private void DoWork()
        {
            string inputRootDir = m_rootDirectory;
            string outputRootDir = Plugin.Config.GetString(NscConfigKey.StandaloneChartsDirectory);

            var setInfos = new List<ChartSetInfo>();
            foreach (string directory in Directory.EnumerateDirectories(inputRootDir))
                WalkDirectory(directory, Path.GetFileName(directory));

            ClientAs<NscClient>().DatabaseWorker.AddRange(setInfos);

            void WalkDirectory(string directory, string currentSetName)
            {
                if (Directory.EnumerateFiles(directory, "*.ksh").Count() > 0)
                    setInfos.Add(ConvertKSHSetAndSave(directory, currentSetName, outputRootDir));
                else foreach (string subDir in Directory.EnumerateDirectories(directory))
                    WalkDirectory(subDir, Path.Combine(currentSetName, Path.GetFileName(subDir)));
            }
        }

        private ChartSetInfo ConvertKSHSetAndSave(string inputSetDir, string setName, string outputRoot)
        {
            Logger.Log($"Creating NSC Chart Set \"{ setName }\" from { inputSetDir }");
            Logger.Block();

            var chartFiles = new List<(string, Chart)>();
            foreach (string kshChartFile in Directory.EnumerateFiles(inputSetDir, "*.ksh"))
            {
                try
                {
                    KshChartMetadata kshMeta;
                    using (var reader = new StreamReader(File.OpenRead(kshChartFile)))
                        kshMeta = KshChartMetadata.Create(reader);

                    var kshChart = KshChart.CreateFromFile(kshChartFile);
                    var chart = kshChart.ToVoltex();

                    chartFiles.Add((kshChartFile, chart));
                }
                catch (Exception) { Logger.Log($"  Failed for { kshChartFile }"); }
            }

            var chartSetInfo = new ChartSetInfo()
            {
                ID = 0, // no database ID, it's not in the database yet
                OnlineID = null, // no online stuff, it's not uploaded

                FilePath = setName,
                FileName = ".theori-set",
            };

            string nscChartDirectory = Path.Combine(outputRoot, setName);
            if (!Directory.Exists(nscChartDirectory))
                Directory.CreateDirectory(nscChartDirectory);

            foreach (var (kshChartFile, chart) in chartFiles)
            {
                string audioFile = Path.Combine(inputSetDir, chart.Info.SongFileName);
                if (File.Exists(audioFile))
                {
                    string audioFileDest = Path.Combine(outputRoot, setName, chart.Info.SongFileName);
                    if (File.Exists(audioFileDest))
                        File.Delete(audioFileDest);
                    File.Copy(audioFile, audioFileDest);
                }

                foreach (var lane in chart.Lanes)
                {
                    foreach (var entity in lane)
                    {
                        switch (entity)
                        {
                            case ButtonEntity button:
                            {
                                if (!button.HasSample) break;

                                string sampleFile = Path.Combine(inputSetDir, button.Sample);
                                if (File.Exists(sampleFile))
                                {
                                    string sampleFileDest = Path.Combine(outputRoot, setName, button.Sample);
                                    if (File.Exists(sampleFileDest))
                                        File.Delete(sampleFileDest);
                                    File.Copy(sampleFile, sampleFileDest);
                                }
                            }
                            break;
                        }
                    }
                }

                string jacketFile = Path.Combine(inputSetDir, chart.Info.JacketFileName);
                if (File.Exists(jacketFile))
                {
                    string jacketFileDest = Path.Combine(outputRoot, setName, chart.Info.JacketFileName);
                    if (File.Exists(jacketFileDest))
                        File.Delete(jacketFileDest);
                    File.Copy(jacketFile, jacketFileDest);
                }

                chart.Info.Set = chartSetInfo;
                chart.Info.FileName = $"{ Path.GetFileNameWithoutExtension(kshChartFile) }.theori";

                chartSetInfo.Charts.Add(chart.Info);
            }

            var s = new ChartSerializer(outputRoot, NeuroSonicGameMode.Instance);
            foreach (var (_, chart) in chartFiles)
                s.SaveToFile(chart);

            var setSerializer = new ChartSetSerializer();
            setSerializer.SaveToFile(outputRoot, chartSetInfo);

            Logger.Unblock();

            return chartSetInfo;
        }
    }
}
