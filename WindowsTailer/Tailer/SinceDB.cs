using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pros.Tailer
{
    class SinceDB
    {
        public enum StartAt { Beginning, End };

        private Dictionary<String, FileData> list = new Dictionary<string, FileData>();
        private object syncObject = new object();
        public void AddEntry(String line)
        {
            var split = line.Split(new char[] { '"' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2) return;
            var desc = split[1].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
            if (desc.Length != 2) return;
            var fd = new FileData()
            {
                FileName = split[0],
                CreateTicks = Int64.Parse(desc[0]),
                Position = Int64.Parse(desc[1]),
            };
            lock (syncObject)
            {
                list[fd.FileName] = fd;
            }
        }
        public void AddNewEntry(String name, long ticks, long position)
        {
            var fd = new FileData()
            {
                FileName = name,
                CreateTicks = ticks,
                Position = position,
                Changed = true,
            };
            lock (syncObject)
            {
                list[fd.FileName] = fd;
            }
        }
        public void DeleteEntry(String name)
        {
            lock (syncObject)
            {
                list.Remove(name);
            }
        }
        public void RenameEntry(String oldName, String newName)
        {
            lock (syncObject)
            {
                var temp = list[oldName];
                temp.FileName = newName;
                temp.Changed = true;
                list.Remove(oldName);
                list[newName] = temp;
            }
        }
        public void MarkEntryChanged(String name)
        {
            lock (syncObject)
            {
                FileData fd;
                if (list.TryGetValue(name, out fd))
                {
                    fd.Changed = true;
                }
            }
        }
        public override string ToString()
        {
            return String.Join(Environment.NewLine, GetValues().Select(v => v.ToString()).ToArray());
        }
        public List<FileData> GetValues()
        {
            List<FileData> data;
            lock (syncObject)
            {
                data = list.Values.ToList();
            }
            return data;
        }
        public void Save(string filename)
        {
            string sdb = this.ToString();
            var fi = new FileInfo(filename);
            if (!fi.Directory.Exists) fi.Directory.Create();
            using (var since = fi.CreateText())
            {
                since.WriteLine(sdb);
            }
        }
        public void Load(string filename)
        {
            var fi = new FileInfo(filename);
            if (fi.Exists)
            {
                string[] lines = File.ReadAllLines(filename);
                foreach (string line in lines)
                {
                    if (line.StartsWith("\""))
                    {
                        AddEntry(line);
                    }
                }
            }
        }
        public void Scan(string directory, string glob, StartAt start)
        {
            var files = Directory.GetFiles(directory, glob, SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                var fi = new FileInfo(file);
                AddNewEntry(file, fi.CreationTimeUtc.Ticks, start == StartAt.Beginning ? 0 : fi.Length);
            }
        }
        public static SinceDB Initialize(string persisted, string directory, string glob, StartAt start)
        {
            SinceDB fileList = new SinceDB();
            fileList.Scan(directory, glob, start);

            if (start == StartAt.End)
            {
                return fileList;
            }

            SinceDB source = new SinceDB();
            if (File.Exists(persisted))
            {
                source.Load(persisted);

                var join = fileList.list.Values.Join(source.list.Values, fd => fd.FileName, fd => fd.FileName, (fde, fds) => new { sourceFD = fds, existingFD = fde });
                foreach (var j in join)
                {
                    FileInfo fi = new FileInfo(j.existingFD.FileName);
                    if (fi.Length > j.sourceFD.Position)
                    {
                        j.existingFD.Position = j.sourceFD.Position;
                        j.existingFD.Changed = true;
                    }
                    else if (fi.Length < j.sourceFD.Position)
                    {
                        j.existingFD.Position = 0;
                        j.existingFD.Changed = true;
                    }
                    else
                    {
                        j.existingFD.Position = j.sourceFD.Position;
                        j.existingFD.Changed = false;
                    }
                }
            }

            return fileList;
        }
    }
}
