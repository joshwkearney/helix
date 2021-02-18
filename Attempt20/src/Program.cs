using Attempt20.Analysis;
using System;
using System.IO;

namespace Attempt20 {
    public class Program {
        public static void Main(string[] args) {
            var file = File.ReadAllText("Resources/Program.txt");

            try {
                var c = new TrophyCompiler(file).Compile();

                Console.WriteLine(c);
                File.WriteAllText("Resources/Output.txt", c);
            }
            catch (TrophyException ex) {
                Console.WriteLine(ex.CreateConsoleMessage(file));
            }

            Console.ReadLine();
        }
    }
}