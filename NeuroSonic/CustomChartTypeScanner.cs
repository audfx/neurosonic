using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using theori.Charting;
using theori.Charting.Serialization;
using theori.Configuration;
using theori.Database;

using NeuroSonic.Charting.KShootMania;
using System;

namespace NeuroSonic
{
    public sealed class CustomChartTypeScanner
    {
        private Task? m_task;

        public void BeginSearching()
        {
            if (m_task != null) return;

            m_task = Task.Run(MainAction);
            //MainAction();
        }

        private void MainAction()
        {
            string chartsDir = TheoriConfig.ChartsDirectory;

            var setGroups = new List<(DirectoryInfo SetDirectory, FileInfo[] Charts)>();
            void EnumerateSubDirs(string parent)
            {
                foreach (string dirPath in Directory.EnumerateDirectories(parent))
                {
                    var dir = new DirectoryInfo(dirPath);

                    var ksh = dir.GetFiles("*.ksh");
                    if (ksh.Length > 0)
                        setGroups.Add((dir, ksh));
                    EnumerateSubDirs(dirPath);
                }
            }

            EnumerateSubDirs(chartsDir);

            var setSer = new ChartSetSerializer(chartsDir);
            foreach (var (setDir, charts) in setGroups)
            {
                var sets = setDir.GetFiles("*.theori-set");
                if (sets.Length == 0)
                {
                    var setInfo = new ChartSetInfo()
                    {
                        FileName = "ksh-auto.theori-set",
                        FilePath = PathL.RelativePath(chartsDir, setDir.FullName),
                    };

                    foreach (var chartFile in charts)
                    {
                        using var reader = File.OpenText(chartFile.FullName);
                        var meta = KshChartMetadata.Create(reader);

                        var chartInfo = new ChartInfo()
                        {
                            Set = setInfo,
                            GameMode = NeuroSonicGameMode.Instance,

                            FileName = chartFile.Name,

                            SongTitle = meta.Title,
                            SongArtist = meta.Artist,
                            SongFileName = meta.MusicFile ?? meta.MusicFileNoFx ?? "??",
                            SongVolume = meta.MusicVolume,
                            ChartOffset = meta.OffsetMillis / 1000.0,
                            Charter = meta.EffectedBy,
                            JacketFileName = meta.JacketPath,
                            JacketArtist = meta.Illustrator,
                            BackgroundFileName = meta.Background,
                            BackgroundArtist = "Unknown",
                            DifficultyLevel = meta.Level,
                            DifficultyIndex = meta.Difficulty.ToDifficultyIndex(chartFile.Name),
                            DifficultyName = meta.Difficulty.ToDifficultyString(chartFile.Name),
                            DifficultyNameShort = meta.Difficulty.ToShortString(chartFile.Name),
                            DifficultyColor = meta.Difficulty.GetColor(chartFile.Name),
                        };

                        setInfo.Charts.Add(chartInfo);
                    }

                    try
                    {
                        setSer.SaveToFile(setInfo);
                        ChartDatabaseService.AddSet(setInfo);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(e.Message);

                        string setFile = Path.Combine(chartsDir, setInfo.FilePath, "ksh-auto.theori-set");
                        if (File.Exists(setFile)) File.Delete(setFile);
                    }
                }
                else
                {
                    // currently assume everything is fine, eventually parse and check for new chart files
                }
            }
        }
    }
}
