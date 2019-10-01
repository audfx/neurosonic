using System;
using System.Diagnostics;
using System.IO;
using theori.Charting;
using theori.Database;

namespace NeuroSonic.Database
{
    public sealed class ChartDatabaseWorker : Disposable
    {
        public enum State
        {
            Idle,

            Populating,
            Cleaning,

            PopulateSearching,
            CleanSearching,
        }

        private readonly ChartDatabase m_database;
        private readonly string m_chartsDir;

        private State m_state = State.Idle;

        private FileSystemWatcher? m_watcher;

        public ChartDatabaseWorker(string localDatabaseName)
        {
            m_database = new ChartDatabase(localDatabaseName);
            m_chartsDir = Plugin.Config.GetString(NscConfigKey.StandaloneChartsDirectory);
        }

        public void PumpEvents()
        {
        }

        public void SetToIdle()
        {
            if (m_state == State.Idle) return;

            switch (m_state)
            {
                case State.Cleaning:
                case State.Populating:
                    m_database.Close();
                    break;
            }

            m_state = State.Idle;
        }

        public void SetToClean()
        {
            if (m_state == State.Cleaning || m_state == State.CleanSearching)
                return; // already set to clean

            if (m_state != State.Idle)
                throw new InvalidOperationException("Database worker is working on another task.");

            m_database.OpenLocal();
            m_state = State.CleanSearching;
        }

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
