using Attempt20.Analysis;
using System;
using System.IO;

namespace Attempt20 {
    public class Program {
        public static void Main(string[] args) {
            try {
                var c = new TrophyCompiler(File.ReadAllText("Resources/Program.txt")).Compile();

                Console.WriteLine(c);
                File.WriteAllText("Resources/Output.txt", c);
            }
            catch (TrophyException ex) {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }
    }
}