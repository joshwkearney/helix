using Attempt17.CodeGeneration;
using Attempt17.Features;
using Attempt17.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Attempt17 {
    public class Program {
        public static void Main(string[] args) {
            var input = File.ReadAllText("program.txt").Replace("\t", "    ");

            try {
                var tokens = new Lexer(input).GetTokens();
                var decls = new Parser(tokens).Parse();
                var gen = new CodeGenerator();
                var scope = new Scope();

                foreach (var decl in decls) {
                    scope = decl.ModifyLateralScope(scope);
                }

                var syntaxDecls = new List<IDeclarationSyntaxTree>();
                foreach (var decl in decls) {
                    decl.ValidateTypes(scope);

                    var syntax = decl.Analyze(scope);

                    syntaxDecls.Add(syntax);
                }

                foreach (var decl in syntaxDecls) {
                    decl.GenerateForwardDeclarations(gen);
                }

                var lines = new List<string>();
                foreach (var decl in syntaxDecls) {
                    lines.AddRange(decl.GenerateCode(gen));
                }

                Console.WriteLine("Header Text ==============================");
                foreach (var line in gen.HeaderLines) {
                    Console.WriteLine(line);
                }
                Console.WriteLine();

                Console.WriteLine("Source Text ==============================");
                foreach (var line in lines) {
                    Console.WriteLine(line);
                }
            }
            catch (CompilerException ex) {
                var loc = ex.Location;
                var (line, start) = GetLineContaining(input, loc.StartIndex);
                var lineNum = input.Substring(0, loc.StartIndex).Count(x => x == '\n') + 1;
                var length = Math.Min(line.Length - start, loc.Length);
                var spaces = new string(Enumerable.Repeat(' ', start).ToArray());
                var arrows = new string(Enumerable.Repeat('^', length).ToArray());

                Console.WriteLine($"Unhandled compilation exception: {ex.Title}");
                Console.WriteLine(ex.Message);
                Console.WriteLine($"at 'program.txt' line {lineNum} pos {start}");
                Console.WriteLine();
                Console.WriteLine(line);
                Console.WriteLine(spaces + arrows);
            }

            Console.ReadLine();
        }

        private static (string line, int index) GetLineContaining(string text, int index) {
            int start = index;
            int end = index;
            int newIndex = 0;

            while (true) {
                if (start == 0) {
                    break;
                }

                if (text[start - 1] == '\n' || text[start - 1] == '\r') {
                    break;
                }

                start--;
                newIndex++;
            }

            while (true) {
                if (end + 1 >= text.Length) {
                    break;
                }

                if (text[end + 1] == '\n' || text[end + 1] == '\r') {
                    break;
                }

                end++;
            }

            var line = text.Substring(start, end - start + 1);

            return (line, newIndex);
        }
    }
}