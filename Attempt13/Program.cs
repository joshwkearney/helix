using System;
using System.IO;

namespace Attempt13 {
    public class Program {
        public static void Main(string[] args) {
            var program = File.ReadAllText("program.txt");
            var parser = new Parser(new Lexer(program));
            var interpreter = new Interpreter();

            var code = parser.Parse();
            var result = interpreter.Interpret(code);

            Console.Read();
        }
    }
}