using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Pros.Tailer
{
    class Tailer
    {
        [Option('g', "glob", DefaultValue = "*", HelpText = "The input file glob to monitor")]
        public string InputGlob { get; set; }

        [Option('d', "directory", DefaultValue = null, HelpText = "The directory to monitor")]
        public string Directory { get; set; }

        [Option('s', "startat", DefaultValue=SinceDB.StartAt.Beginning, HelpText="Start at the beginning or the end of the files")]
        public SinceDB.StartAt Start { get; set; }

        [Option('t', "tracking", DefaultValue = ".sincedb", HelpText = "The file to store the last read database in")]
        public String StatusFile { get; set; }

        [Option('h', "help", DefaultValue=false, HelpText="Display this help message")]
        public bool Help { get; set; }

        [Option('m', "monitor", HelpText="Monitor this process id for failure")]
        public int RemoteProcess { get; set; }

        [Option('i', "interval", DefaultValue = 500, HelpText = "Scan interval")]
        public int Interval { get; set; }

        [Option('e', "exclude", HelpText = "Exclude these patterns")]
        public IEnumerable<string> Excludes { get; set; }

        public bool IsValid { get; private set; }
        public bool Quit { get; private set; }

        private SinceDB db;
        private Watcher watcher;
        private List<Regex> ExcludeList = (new string[] { ".*\\.gz", ".*\\.zip", ".*\\.tar", "temp.*$" })
            .Select(s=>new Regex(s, RegexOptions.IgnoreCase))
            .ToList();

        static void Main(string[] args)
        {
            var myself = new Tailer(args);
            if (myself.IsValid)
            {
                myself.Initialize();
                myself.Run();
            }
            Console.ReadKey();
        }

        public Tailer(string[] args)
        {
            IsValid = CommandLine.Parser.Default.ParseArguments(args, this);
            if (IsValid && !Help)
            {
                if (String.IsNullOrEmpty(Directory)) Directory = Environment.CurrentDirectory;
                StatusFile = new FileInfo(StatusFile).FullName;
                if (Interval <= 0) Interval = 500;
                Quit = false;
                new Thread(Monitor).Start();
            }
            else
            {
                IsValid = false;
                Console.WriteLine(HelpText.AutoBuild(this));
            }
        }

        private void Initialize()
        {
            if (Excludes != null && Excludes.Count() > 0)
            {
                ExcludeList.AddRange(Excludes.Select(s => new Regex(s, RegexOptions.IgnoreCase)));
            }
            db = SinceDB.Initialize(StatusFile, Directory, InputGlob, Start);
            watcher = new Watcher(Directory, InputGlob);
        }

        private void Run()
        {
            watcher.Start(db);
            DateTime startDate = DateTime.Now;
            while (!Quit) {
                Scan();
                Thread.Sleep(Interval);
            }
        }

        private void Scan()
        {
            foreach (FileData fd in db.GetValues().Where(f => f.Changed))
            {
                FileInfo fi = new FileInfo(fd.FileName);
                if (fd.FileName.Equals(StatusFile)) continue;
                if (ExcludeList.Exists(r => r.IsMatch(fi.Name))) continue;

                fd.Changed = false;
                try
                {
                    if (fi.Length < fd.Position) fd.Position = 0;
                    if (fi.Length == fd.Position) continue;
                    using (StreamReader sr = new StreamReader(fi.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete), Encoding.UTF8))
                    {
                        sr.BaseStream.Seek(fd.Position, SeekOrigin.Begin);
                        using (var writer = new Newtonsoft.Json.JsonTextWriter(Console.Out))
                        {
                            writer.CloseOutput = false;
                            writer.Formatting = Newtonsoft.Json.Formatting.None;
                            string text = sr.ReadLine();
                            while (text != null && !Quit)
                            {
                                WriteJson(writer, fi.FullName, text);
                                Console.WriteLine();
                                text = sr.ReadLine();
                            }
                        }
                        fd.Position = sr.BaseStream.Position;
                    }
                    db.Save(StatusFile);
                }
                catch (IOException)
                {
                    continue;
                }
                if (Quit) break;
            }
        }

        private static void WriteJson(Newtonsoft.Json.JsonTextWriter writer, String name, string text)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("@message");
            writer.WriteValue(text);
            writer.WritePropertyName("@source");
            writer.WriteValue(name);
            writer.WriteEndObject();
        }

        private void Monitor()
        {
            if (RemoteProcess > 0)
            {
                try
                {
                    using (var p = Process.GetProcessById(RemoteProcess))
                    {
                        p.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Exception monitoring external process: {0}", ex.Message);
                }
                Quit = true;
            }
        }
    }
}
