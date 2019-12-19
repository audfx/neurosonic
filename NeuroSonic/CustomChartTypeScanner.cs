using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

using theori.Charting;
using theori.Charting.Serialization;
using theori.Configuration;
using theori.Database;

using NeuroSonic.Charting.KShootMania;
using System;
using System.Threading;

namespace NeuroSonic
{
    public sealed class CustomChartTypeScanner
    {
        private Task? m_searchTask, m_convertTask;
        private int m_attempts = 0;

        private readonly ConcurrentQueue<(DirectoryInfo SetDirectory, FileInfo[] Charts)> m_queue = new ConcurrentQueue<(DirectoryInfo, FileInfo[])>();

        public void BeginSearching()
        {
            if (m_searchTask != null) return;

            m_searchTask = Task.Run(SearchAction);
            //MainAction();
        }

        public void Update()
        {
            if (m_searchTask != null && m_searchTask.IsCompleted) m_searchTask = null;
            if (m_convertTask != null && m_convertTask.IsCompleted) m_convertTask = null;
        }

        private void SearchAction()
        {
            string chartsDir = TheoriConfig.ChartsDirectory;
            if (!Directory.Exists(chartsDir)) return;

            void EnumerateSubDirs(string parent)
            {
                foreach (string dirPath in Directory.EnumerateDirectories(parent))
                {
                    var dir = new DirectoryInfo(dirPath);

                    var ksh = dir.GetFiles("*.ksh");
                    if (ksh.Length > 0)
                        m_queue.Enqueue((dir, ksh));
                    EnumerateSubDirs(dirPath);
                }
            }

            EnumerateSubDirs(chartsDir);

            m_convertTask = Task.Run(ConvertAction);
        }

        private void ConvertAction()
        {
            string chartsDir = TheoriConfig.ChartsDirectory;
            if (!Directory.Exists(chartsDir)) return;

            var setSer = new ChartSetSerializer(chartsDir);
            while (true)
            {
                while (m_queue.IsEmpty)
                {
                    Thread.Sleep(1000);
                    m_attempts++;

                    if (m_attempts > 5) return;
                }

                if (!m_queue.TryDequeue(out var entry)) continue;
                var (setDir, charts) = entry;

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
