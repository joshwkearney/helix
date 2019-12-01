using System;

namespace Attempt4 {
    class Program {
        static void Main(string[] args) {
            var parser = new Parser("9 * (2 + 3) / 3");
            var analyzer = new Analyzer(parser.Parse());
            //var interpreter = new Interpreter(analyzer.Analyze());
            //var result = interpreter.Interpret();

            Console.WriteLine(analyzer.Analyze());
            Console.Read();
        }
    }
}