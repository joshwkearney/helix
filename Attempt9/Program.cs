using System;
using System.IO;

namespace Attempt9 {
    public class Program {
        public static void Main(string[] args) {
            Lexer lex = new Lexer(File.ReadAllText("program.txt"));
            Parser parse = new Parser(lex);
            Analyzer ana = new Analyzer(parse.Parse());
            var code = new CodeGenerator(ana.Analyze()).Generate();

            Console.WriteLine(code);
            Console.Read();
        }
    }
}