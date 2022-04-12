using System;
using System.Diagnostics;

namespace BnsBinTool.Core
{
    public static class Logger
    {
        private static readonly Stopwatch _stopwatch = new Stopwatch();

        public static bool Minimal { get; set; }

        public static void Log(string text)
        {
            if (Minimal)
                Console.WriteLine(text);
            else
            {
                var usage = Environment.WorkingSet / 1024 / 1024 + " MiB";
                Console.WriteLine($"[{usage.PadRight(10)}] {text}");
            }
        }

        public static void LogTime(string text)
        {
            if (Minimal)
                Console.WriteLine(text);
            else
            {
                var usage = Environment.WorkingSet / 1024 / 1024 + " MiB";
                Console.WriteLine($"[{usage.PadRight(10)}] [{_stopwatch.Elapsed}] {text}");
            }
        }

        public static void LogTime(object text)
        {
            LogTime(text.ToString());
        }

        public static void StartTimer()
        {
            _stopwatch.Restart();
        }

        public static void StopTimer()
        {
            _stopwatch.Stop();
        }
    }
}