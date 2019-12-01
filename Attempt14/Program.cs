using System;
using System.IO;

namespace Attempt14 {
    public class Program {
        public static void Main(string[] args) {
            string text = File.ReadAllText("program.txt");
            Parser parse = new Parser(new Lexer(text));

            var code = parse.Parse();
            var result = new StackInterpreter().Interpret(code);

            Console.WriteLine(new Serializer().Serialize(result));
            Console.Read();
        }
    }
}