using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Attempt20 {
    public class Program {
        public static void Main(string[] args) {
            try {
                var c = new Compiler(File.ReadAllText("Resources/Program.txt")).Compile();

                Console.WriteLine(c);
                File.WriteAllText("Resources/Output.txt", c);
            }
            catch (CompilerException ex) {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }
    }
}