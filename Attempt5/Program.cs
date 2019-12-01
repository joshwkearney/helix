using Attempt6.Analyzing;
using Attempt6.Compiling;
using Attempt6.Lexing;
using Attempt6.Parsing;
using System;
using System.IO;

namespace Attempt6 {
    public class Program {
        public static void Main(string[] args) {
            Lexer lexer = new Lexer(File.ReadAllText("program.txt"));
            Parser parser = new Parser(lexer);
            SemanticAnalyzer analyzer = new SemanticAnalyzer(parser.Parse());
            Compiler compiler = new Compiler(analyzer.Analyze());

            compiler.Compile();

            Console.Read();
        }
    }
}