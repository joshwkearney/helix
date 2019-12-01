using Attempt12.Analyzing;
using System;
using System.IO;

namespace Attempt12 {
    public class Program {
        public static void Main(string[] args) {
            Lexer lexer = new Lexer(File.ReadAllText("program.txt"));
            Parser parser = new Parser(lexer);

            Analyzer analyzer = new Analyzer(parser.Parse());
            Interpreter interpreter = new Interpreter(analyzer.Analyze());

            Console.WriteLine(interpreter.Interpret());

            Console.Read();
        }
    }
}