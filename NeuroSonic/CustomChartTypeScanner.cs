using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using theori.Charting;
using theori.Charting.Serialization;
using theori.Configuration;
using theori.Database;

using NeuroSonic.Charting.KShootMania;

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
            string chartsDir = Path.GetFullPath(TheoriConfig.ChartsDirectory);
            Logger.Log($"Searching for .ksh files in `{chartsDir}`");
            if (!Directory.Exists(chartsDir))
            {
                Logger.Log($"Directory not found: `{chartsDir}`");
                return;
            }

            void EnumerateSubDirs(string parent)
            {
                Logger.Log($"Searching for .ksh chart groups in `{parent}`");
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

            Logger.Log($"Completed search for .ksh files; converting directories to .theori-set");
            m_convertTask = Task.Run(ConvertAction);
        }

        private void ConvertAction()
        {
            string chartsDir = Path.GetFullPath(TheoriConfig.ChartsDirectory);
            if (!Directory.Exists(chartsDir)) return;

            var setSer = new ChartSetSerializer(chartsDir);
            while (true)
            {
                while (m_queue.IsEmpty)
                {
                    Thread.Sleep(500);
                    m_attempts++;

                    if (m_attempts > 15) return;
                }

                if (!m_queue.TryDequeue(out var entry))
                {
                    Logger.Log($"Attempted to load charts from the queue, none found. Retrying");
                    m_attempts++;
                    continue;
                }
                var (setDir, charts) = entry;

                var sets = setDir.GetFiles("*.theori-set");
                if (sets.Length == 0)
                {
                    var setInfo = new ChartSetInfo()
                    {
                        FileName = "ksh-auto.theori-set",
                        FilePath = PathL.RelativePath(chartsDir, setDir.FullName),
                    };
                    Logger.Log($"No .theori-set file present for .ksh group in `{setDir}`; creating one at {setInfo.FilePath}.");

                    Logger.Log($"Adding {charts.Length} charts...");
                    foreach (var chartFile in charts)
                    {
                        Logger.Log($"Adding `{chartFile}` to the new chart set.");
                        using var reader = File.OpenText(chartFile.FullName);
                        Logger.Log($"  Opened text file for reading.");
                        var meta = KshChartMetadata.Create(reader);
                        Logger.Log($"  Got KSH metadata.");

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

                        Logger.Log($"  Created info, adding to set.");
                        setInfo.Charts.Add(chartInfo);
                    }
                    Logger.Log("Done adding charts, ready to save.");

                    try
                    {
                        Logger.Log("Saving the chart set to file...");
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
                    //Logger.Log(".theori-set file already exists, skipping.");
                    // currently assume everything is fine, eventually parse and check for new chart files
                }
            }
        }
    }
}
