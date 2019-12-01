using Attempt12.DataFormat;
using Attempt12.Language;
using Attempt12.Serialization;
using System;
using System.IO;

namespace Attempt12 {
    class Program {
        static void Main(string[] args) {
            string file = File.ReadAllText("program.txt");
            Parser parser = new Parser(new Lexer(file));

            var data = parser.Parse();

            LanguageInterpreter pret = new LanguageInterpreter();
            var result = pret.Interpret(data);

            Serializer serial = new Serializer();
            Console.WriteLine(serial.Serialize(result));

            Console.Read();
        }
    }
}