using System;

namespace Attempt3 {
    class Program {
        static void Main(string[] args) {
            Parser tok = new Parser("2 * -(1 + 3)");
            Analyzer analyzer = new Analyzer(tok.Tokenize());

            var result = analyzer.Analyze();

            Console.WriteLine(result);
            Console.Read();
        }
    }
}
