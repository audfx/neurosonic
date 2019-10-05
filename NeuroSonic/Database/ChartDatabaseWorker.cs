using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using theori.Charting;
using theori.Database;

namespace NeuroSonic.Database
{
    public sealed class ChartDatabaseWorker : Disposable
    {
        public enum WorkState
        {
            Idle,

            Populating,
            Cleaning,

            PopulateSearching,
            CleanSearching,
        }

        private readonly ChartDatabase m_database;
        private readonly string m_chartsDir;

        public WorkState State { get; private set; } = WorkState.Idle;

        private CancellationTokenSource? m_currentTaskCancellation;
        private Task<Queue<ChartSetInfo>>? m_currentSearchTask;
        private Queue<ChartSetInfo>? m_populateQueue;

        private FileSystemWatcher? m_watcher;

        public IEnumerable<ChartSetInfo> ChartSets => m_database.ChartSets;
        public IEnumerable<ChartInfo> Charts => m_database.Charts;

        public ChartDatabaseWorker(string localDatabaseName)
        {
            m_database = new ChartDatabase(localDatabaseName);
            m_chartsDir = Plugin.Config.GetString(NscConfigKey.StandaloneChartsDirectory);

            m_database.OpenLocal();
        }

        public void Update()
        {
            switch (State)
            {
                case WorkState.PopulateSearching:
                {
                    if (!m_currentSearchTask!.IsCompleted) break;

                    m_currentTaskCancellation = null; // no need to cancel, it's completed
                    if (m_currentSearchTask.IsCompletedSuccessfully)
                    {
                        Logger.Log("Setting database worker to Populate");
                        State = WorkState.Populating;

                        m_populateQueue = m_currentSearchTask.Result;
                        m_currentSearchTask = null;
                    }
                    else SetToIdle();
                } break;

                case WorkState.Populating:
                {
                    int maxWorkCount = 10;
                    while (maxWorkCount-- > 0 && m_populateQueue!.TryDequeue(out var info))
                    {
                        Logger.Log($"Processing { info.FilePath }");
                        //if (m_database.ContainsSet(info)) continue;
                        m_database.AddSet(info);
                    }

                    if (m_populateQueue!.Count == 0)
                        SetToIdle();
                } break;

                case WorkState.CleanSearching:
                {
                    foreach (var set in m_database.ChartSets)
                    {
                        string dirPath = Path.Combine(m_chartsDir, set.FilePath);
                        if (!Directory.Exists(dirPath))
                            m_database.RemoveSet(set);
                    }

                    Logger.Log("Setting database worker to Clean");
                    State = WorkState.Cleaning;
                }
                break;

                case WorkState.Cleaning:
                {
                    SetToIdle();
                }
                break;
            }
        }

        public void SetToIdle()
        {
            if (State == WorkState.Idle) return;
            Logger.Log("Setting database worker to Idle");

            switch (State)
            {
                case WorkState.CleanSearching:
                    break;

                case WorkState.PopulateSearching:
                    m_currentTaskCancellation?.Cancel();
                    m_currentTaskCancellation = null;

                    m_currentSearchTask = null;
                    m_populateQueue = null;
                    break;

                case WorkState.Cleaning:
                case WorkState.Populating:
                    break;
            }

            State = WorkState.Idle;
        }

        public void AddRange(IEnumerable<ChartSetInfo> setInfos)
        {
            if (State != WorkState.Idle)
                throw new InvalidOperationException("Database worker is working on another task.");

            m_populateQueue = new Queue<ChartSetInfo>(setInfos);
            State = WorkState.Populating;
        }

        public void SetToPopulate()
        {
            if (State == WorkState.Populating || State == WorkState.PopulateSearching)
                return; // already set to clean

            if (State != WorkState.Idle)
                throw new InvalidOperationException("Database worker is working on another task.");

            Logger.Log("Setting database worker to Populate (searching)");

            //m_database.OpenLocal();
            State = WorkState.PopulateSearching;

            m_currentTaskCancellation = new CancellationTokenSource();
            m_currentSearchTask = Task.Run(() => GetChartSetsInDirectory(m_chartsDir, m_currentTaskCancellation.Token));
        }

        private static Queue<ChartSetInfo> GetChartSetsInDirectory(string chartsDirectory, CancellationToken ct)
        {
            var result = new Queue<ChartSetInfo>();

            var setSerializer = new ChartSetSerializer();
            SearchDirectory(chartsDirectory, null);

            return result;

            void SearchDirectory(string directory, string? currentSubDirectory)
            {
                foreach (string entry in Directory.EnumerateDirectories(directory))
                {
                    if (ct.IsCancellationRequested)
                        ct.ThrowIfCancellationRequested();

                    string entrySubDirectory = currentSubDirectory == null ? Path.GetFileName(entry) : Path.Combine(currentSubDirectory, Path.GetFileName(entry));
                    // TODO(local): check for anything eith any .theori-set extension
                    if (File.Exists(Path.Combine(entry, ".theori-set")))
                        result.Enqueue(setSerializer.LoadFromFile(chartsDirectory, entrySubDirectory, ".theori-set"));
                    else SearchDirectory(entry, entrySubDirectory);
                }
            }
        }

        public void SetToClean()
        {
            if (State == WorkState.Cleaning || State == WorkState.CleanSearching)
                return; // already set to clean

            if (State != WorkState.Idle)
                throw new InvalidOperationException("Database worker is working on another task.");

            Logger.Log("Setting database worker to Clean (searching)");

            //m_database.OpenLocal();
            State = WorkState.CleanSearching;
        }

        private static void CleanDatabaseOfMissingEntries()
        {
        }

        #region File System Watcher

        private void WatchForFileSystemChanges()
        {
            if (m_watcher != null) return;

            var watcher = new FileSystemWatcher(m_chartsDir)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                Filter = "*.theori-set|*.theori",
            };

            watcher.Changed += Watcher_Changed;
            watcher.Deleted += Watcher_Changed;
            watcher.Created += Watcher_Changed;
            watcher.Renamed += Watcher_Renamed;

            m_watcher = watcher;
            watcher.EnableRaisingEvents = true;
        }

        private void StopWatchingForFileSystemChanges()
        {
            if (m_watcher == null) return;

            m_watcher.Dispose();
            m_watcher = null;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
        }

        #endregion

        private void AddSetFileRelative(string relPath)
        {
            if (PathL.IsFullPath(relPath))
                throw new ArgumentException($"{ nameof(AddSetFileRelative) } expects a relative path.");

            string setDir = Directory.GetParent(relPath).FullName;
            string setFile = Path.GetFileName(relPath);

            Debug.Assert(Path.Combine(setDir, setFile) == relPath);

            var setSerializer = new ChartSetSerializer();
            var setInfo = setSerializer.LoadFromFile(m_chartsDir, setDir, setFile);

            m_database.AddSet(setInfo);
        }

        private void AddSetFile(string fullPath)
        {
            if (!PathL.IsFullPath(fullPath))
                throw new ArgumentException($"{ nameof(AddSetFile) } expects a full path and will convert it to a relative path.");

            string relPath;
            try
            {
                relPath = PathL.RelativePath(fullPath, m_chartsDir);
            }
            catch (ArgumentException e)
            {
                Logger.Log(e);
                return;
            }

            AddSetFileRelative(relPath);
        }
    }
}
