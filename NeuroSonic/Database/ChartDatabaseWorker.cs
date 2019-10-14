using System;
using System.Collections.Concurrent;
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

            //PopulateSearching,
            CleanSearching,
        }

        private readonly ChartDatabase m_database;
        private readonly string m_chartsDir;

        public WorkState State { get; private set; } = WorkState.Idle;

        private CancellationTokenSource? m_currentTaskCancellation;

        private Task? m_populateSearchTask, m_populateTask;
        private readonly ConcurrentQueue<ChartSetInfo> m_populateQueue = new ConcurrentQueue<ChartSetInfo>();

        private Action? m_onSetToIdleCallback = null;

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
                case WorkState.Populating:
                {
                    if (m_populateSearchTask != null && m_populateSearchTask.IsCompleted)
                        m_populateSearchTask = null;

                    if (m_populateTask != null && m_populateTask.IsCompleted)
                        m_populateTask = null;

                    if (m_populateQueue.Count > 0 && m_populateTask == null)
                    {
                        Debug.Assert(m_currentTaskCancellation != null);
                        m_populateTask = Task.Run(() => RunPopulate(m_currentTaskCancellation!.Token));
                    }

                    if (m_populateSearchTask == null && m_populateQueue.Count == 0)
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

                case WorkState.Populating:
                    m_currentTaskCancellation?.Cancel();
                    m_currentTaskCancellation = null;

                    m_populateTask = m_populateSearchTask = null;
                    break;

                case WorkState.Cleaning:
                    break;
            }

            State = WorkState.Idle;

            m_onSetToIdleCallback?.Invoke();
            m_onSetToIdleCallback = null;
        }

        public void AddRange(IEnumerable<ChartSetInfo> setInfos)
        {
            if (State != WorkState.Idle)
                throw new InvalidOperationException("Database worker is working on another task.");

            Debug.Assert(m_populateTask == null);

            m_currentTaskCancellation = new CancellationTokenSource();

            foreach (var info in setInfos)
                m_populateQueue.Enqueue(info);
            State = WorkState.Populating;
        }

        public void SetToPopulate(Action? onIdle = null)
        {
            if (State == WorkState.Populating)
                return; // already set to clean

            if (State != WorkState.Idle)
                throw new InvalidOperationException("Database worker is working on another task.");

            Logger.Log("Setting database worker to Populate (searching)");

            //m_database.OpenLocal();
            m_onSetToIdleCallback = onIdle;
            State = WorkState.Populating;

            m_currentTaskCancellation = new CancellationTokenSource();
            m_populateSearchTask = Task.Run(() => RunPopulateSearch(m_currentTaskCancellation.Token));
        }

        private void EnqueuePopulateEntry(ChartSetInfo setInfo)
        {
            m_populateQueue!.Enqueue(setInfo);
        }

        private void RunPopulateSearch(CancellationToken ct)
        {
            string chartsDirectory = m_chartsDir;

            var setSerializer = new ChartSetSerializer();
            SearchDirectory(chartsDirectory, null);

            void SearchDirectory(string directory, string? currentSubDirectory)
            {
                foreach (string entry in Directory.EnumerateDirectories(directory))
                {
                    if (ct.IsCancellationRequested)
                        ct.ThrowIfCancellationRequested();

                    string entrySubDirectory = currentSubDirectory == null ? Path.GetFileName(entry) : Path.Combine(currentSubDirectory, Path.GetFileName(entry));
                    // TODO(local): check for anything eith any .theori-set extension
                    if (File.Exists(Path.Combine(entry, ".theori-set")))
                    {
                        // TODO(local): see if this can be updated rather than just skipped
                        if (m_database.ContainsSetAtLocation(Path.Combine(entrySubDirectory, ".theori-set"))) continue;
                        EnqueuePopulateEntry(setSerializer.LoadFromFile(chartsDirectory, entrySubDirectory, ".theori-set"));
                    }
                    else SearchDirectory(entry, entrySubDirectory);
                }
            }
        }

        private void RunPopulate(CancellationToken ct)
        {
            while (m_populateQueue!.TryDequeue(out var info))
            {
                if (ct.IsCancellationRequested)
                    ct.ThrowIfCancellationRequested();

                Logger.Log($"Adding { info.FilePath } to the database");
                //if (m_database.ContainsSet(info)) continue;
                m_database.AddSet(info);
            }
        }

        public void SetToClean(Action? onIdle = null)
        {
            if (State == WorkState.Cleaning || State == WorkState.CleanSearching)
                return; // already set to clean

            if (State != WorkState.Idle)
                throw new InvalidOperationException("Database worker is working on another task.");

            Logger.Log("Setting database worker to Clean (searching)");

            //m_database.OpenLocal();
            m_onSetToIdleCallback = onIdle;
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
