using System.Collections.Generic;
using System.IO;

using theori;
using theori.Charting;
using theori.Charting.Serialization;
using theori.IO;

using NeuroSonic.Startup;
using NeuroSonic.GamePlay;
using NeuroSonic.Charting.KShootMania;
using NeuroSonic.Charting.Conversions;
using NeuroSonic.Charting;
using NeuroSonic.Platform;

namespace NeuroSonic.ChartSelect
{
    internal class ChartManagerLayer : BaseMenuLayer
    {
        protected override string Title => "Chart Manager";

        private readonly string m_chartsDir = Plugin.Config.GetString(NscConfigKey.StandaloneChartsDirectory);
        
        private bool m_inGame = false;

        protected override void GenerateMenuItems()
        {
            AddMenuItem(new MenuItem(NextOffset, "Go To Chart Select", () => Push(new ChartSelectLayer(ClientSkinService.CurrentlySelectedSkin))));

            AddSpacing();
            AddMenuItem(new MenuItem(NextOffset, "Convert KSH Charts and Open Selected", () =>
            {
                AutoPlay autoPlay = Keyboard.IsDown(KeyCode.LCTRL) || Keyboard.IsDown(KeyCode.RCTRL) ? AutoPlay.ButtonsAndLasers : AutoPlay.None;

                Push(new FileSystemBrowser(paths =>
                {
                    if (paths == null) return; // selection cancelled?? idk I guess just backing out is a cancel.

                    ClientAs<NscClient>().CloseCurtain(() =>
                    {
                        string primaryKshFile = paths[0];
                        var chartSetInfo = ConvertKSHAndSave(primaryKshFile, out ChartInfo selected);

                        Push(new GameLayer(ClientSkinService.CurrentlySelectedSkin, selected, autoPlay));
                    });
                }));
            }));

            AddSpacing();
            AddMenuItem(new MenuItem(NextOffset, "Delete NSC Chart Database", () =>
            {
                if (File.Exists("nsc-local.chart-db"))
                    File.Delete("nsc-local.chart-db");
            }));
        }

        public override void Resumed(Layer previousLayer)
        {
            base.Resumed(previousLayer);

            if (m_inGame)
            {
                ClientAs<NscClient>().OpenCurtain();
                m_inGame = false;
            }
        }
        private ChartSetInfo ConvertKSHAndSave(string primaryKshFile, out ChartInfo selected)
        {
            var primaryKshChart = KshChart.CreateFromFile(primaryKshFile);
            var primaryChart = primaryKshChart.ToVoltex();

            string setDir = Directory.GetParent(primaryKshFile).FullName;
            // Since we can't know where the parent 'charts' directory might be for this chart is,
            //  or even if it exists, when converting this way we only care that the first directory
            //  up is the name of the set directory rather than the whole path thru a 'charts' directory.
            // A more feature-complete converter will instead ask where the root charts directory is and do this more accurately.
            string setName = Path.GetFileName(setDir);

            var chartFiles = new List<(string, Chart)> { (primaryKshFile, primaryChart) };
            foreach (string kshChartFile in Directory.EnumerateFiles(setDir, "*.ksh"))
            {
                if (Path.GetFileName(kshChartFile) == Path.GetFileName(primaryKshFile)) continue;

                KshChartMetadata kshMeta;
                using (var reader = new StreamReader(File.OpenRead(kshChartFile)))
                    kshMeta = KshChartMetadata.Create(reader);

                var kshChart = KshChart.CreateFromFile(kshChartFile);
                var chart = kshChart.ToVoltex();

                chartFiles.Add((kshChartFile, chart));
            }

            var chartSetInfo = new ChartSetInfo()
            {
                ID = 0, // no database ID, it's not in the database yet
                OnlineID = null, // no online stuff, it's not uploaded

                FilePath = setName,
                FileName = ".theori-set",
            };

            string nscChartDirectory = Path.Combine(m_chartsDir, setName);
            if (!Directory.Exists(nscChartDirectory))
                Directory.CreateDirectory(nscChartDirectory);

            foreach (var (kshChartFile, chart) in chartFiles)
            {
                string audioFile = Path.Combine(setDir, chart.Info.SongFileName);
                if (File.Exists(audioFile))
                {
                    string audioFileDest = Path.Combine(m_chartsDir, setName, chart.Info.SongFileName);
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

                                string sampleFile = Path.Combine(setDir, button.Sample);
                                if (File.Exists(sampleFile))
                                {
                                    string sampleFileDest = Path.Combine(m_chartsDir, setName, button.Sample);
                                    if (File.Exists(sampleFileDest))
                                        File.Delete(sampleFileDest);
                                    File.Copy(sampleFile, sampleFileDest);
                                }
                            }
                            break;
                        }
                    }
                }

                string jacketFile = Path.Combine(setDir, chart.Info.JacketFileName);
                if (File.Exists(jacketFile))
                {
                    string jacketFileDest = Path.Combine(m_chartsDir, setName, chart.Info.JacketFileName);
                    if (File.Exists(jacketFileDest))
                        File.Delete(jacketFileDest);
                    File.Copy(jacketFile, jacketFileDest);
                }

                chart.Info.Set = chartSetInfo;
                chart.Info.FileName = $"{ Path.GetFileNameWithoutExtension(kshChartFile) }.theori";

                chartSetInfo.Charts.Add(chart.Info);
            }

            selected = primaryChart.Info;

            var s = new ChartSerializer(m_chartsDir, NeuroSonicGameMode.Instance);
            foreach (var (_, chart) in chartFiles)
                s.SaveToFile(chart);

            var setSerializer = new ChartSetSerializer();
            setSerializer.SaveToFile(m_chartsDir, chartSetInfo);

            return chartSetInfo;
        }
    }
}
