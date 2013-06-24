using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pros.Tailer
{
    class Watcher
    {
        private FileSystemWatcher watcher = new FileSystemWatcher();
        private SinceDB db;

        public Watcher(String directory, String glob)
        {
            watcher.Path = directory;
            watcher.Filter = glob;
            watcher.IncludeSubdirectories = false;
            watcher.Changed += Changed;
            watcher.Created += Created;
            watcher.Deleted += Deleted;
            watcher.Renamed += Renamed;
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
        }

        public void Start(SinceDB db)
        {
            this.db = db;
            watcher.EnableRaisingEvents = true;
        }

        private void Renamed(object sender, RenamedEventArgs e)
        {
            if (db != null)
            {
                db.RenameEntry(e.OldFullPath, e.FullPath);
            }
        }

        private void Deleted(object sender, FileSystemEventArgs e)
        {
            if (db != null)
            {
                db.DeleteEntry(e.FullPath);
            }
        }

        private void Created(object sender, FileSystemEventArgs e)
        {
            if (db != null)
            {
                db.AddNewEntry(e.FullPath, 0, 0);
            }
        }

        private void Changed(object sender, FileSystemEventArgs e)
        {
            if (db != null)
            {
                db.MarkEntryChanged(e.FullPath);
            }
        }
    }
}
