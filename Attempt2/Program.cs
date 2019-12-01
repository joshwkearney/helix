using Attempt2.Compiling;
using Attempt2.Lexing;
using Attempt2.Parsing;
using System;
using System.IO;

namespace Attempt2 {
    class Program {
        static void Main(string[] args) {
            Lexer lexer = new Lexer(File.ReadAllText("Program.txt"));
            Parser parse = new Parser(lexer);
            Compiler compiler = new Compiler(parse.Parse());

            string result = compiler.Compile();

            Console.Read();
        }
    }
}