using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;

namespace TailerTest
{
    class Program
    {
        private static string FileName = "TestFile";
        private static FileShare writeShare = FileShare.ReadWrite;

        public enum StartAt { Beginning, End };

        static void Main(string[] args)
        {
            Run();
            Console.ReadKey();
        }

        /// <summary>
        /// need to keep track of the file list
        /// load from file if the file exists
        /// save file
        /// </summary>
        static void Run()
        {
            var fi = new FileInfo(FileName);
            if (!fi.Exists) { using (fi.Create()) { } }
            if (File.Exists(FileName + 1)) File.Delete(FileName + 1);
            if (File.Exists(FileName + 2)) File.Delete(FileName + 2);

            var t = new Thread(() => { WriteFile(100); });
            t.Start();
            t.Join();
        }

        private static void WriteFile(int count)
        {
            var r = new Random(1200);
            for (int i = 0; i < count; i++)
            {
                var fi = new FileInfo(FileName);
                using (var fs = new StreamWriter(fi.Open(FileMode.Append, FileAccess.Write, writeShare), Encoding.UTF8))
                {
                    for (int j = 0; j < 100; j++)
                    {
                        fs.WriteLine(j);
                    }
                }

                if (r.Next(5) == 0)
                {
                    string fn2 = FileName + 2;
                    string fn1 = FileName + 1;
                    if (File.Exists(fn2)) File.Delete(fn2);
                    if (File.Exists(fn1)) File.Move(fn1, fn2);
                    fi.MoveTo(fn1);
                }
                Thread.Sleep(91);
            }
        }
    }
}
