using Attempt7.Lexing;
using Attempt7.Parsing;
using System;
using System.IO;

namespace Attempt7 {
    public class Program {
        public static void Main(string[] args) {
            Lexer lexer = new Lexer(File.ReadAllText("program.txt"));
            Parser parser = new Parser(lexer);

            Console.WriteLine(parser.Parse().Interpret().Result);

            Console.Read();
        }
    }
}