using System;
using System.Diagnostics;
using System.IO;

namespace Trophy {
    public class Program {
        public static void Main(string[] args) {
            var file = File.ReadAllText("../../../resources/Parser7.trophy");
            var watch = new Stopwatch();

            watch.Start();
            long time = 0;

            try {
                var c = new TrophyCompiler(file).Compile();
                time = watch.ElapsedMilliseconds;

                Console.WriteLine(c);
                Console.WriteLine("Compiled 'Program.txt' in " + time + " ms");
            }
            catch (TrophyException) { }

            // Console.WriteLine();
            //File.WriteAllText("../../../resources/Output.txt", c);
        }
    }
}