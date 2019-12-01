using Attempt6.Evaluating;
using Attempt6.Lexing;
using Attempt6.Parsing;
using System;
using System.IO;

namespace Attempt6 {
    public class Program {
        public static void Main(string[] args) {
            Lexer lexer = new Lexer(File.ReadAllText("program.txt"));
            Reader reader = new Reader(lexer);
            Parser parser = new Parser(reader.Read());
            var (ast, builtins) = parser.Parse();
            Evaluator eval = new Evaluator(ast, builtins);

            Console.WriteLine(eval.Evalutate());
            Console.Read();
        }
    }
}