using JoshuaKearney.FileSystem;
using System;
using System.IO;

namespace Attempt10 {
    public class Program {
        public static void Main(string[] args) {
            Lexer lex = new Lexer(File.ReadAllText("program.trophy"));
            Parser parse = new Parser(lex);

            var tree = parse.Parse();//.GetSyntax(new Scope());
            CodeGenerator gen = new CodeGenerator(tree);

            var file = StoragePath.CurrentDirectory + @"..\..\..\..\" + @"CTesting\Program.c";
            File.WriteAllText(file.ToString(), gen.Generate());
        }
    }
}