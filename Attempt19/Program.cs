using Attempt18.CodeGeneration;
using Attempt18.Compiling;
using Attempt18.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Attempt18 {
    public class Program {
        public static void Main(string[] args) {
            var input = File.ReadAllText("program.txt").Replace("\t", "    ");

            try {
                var sw = new Stopwatch();
                sw.Start();

                var comp = new Compiler();
                var result = comp.Compile(input);

                sw.Stop();

                Console.WriteLine("Compiled in " + sw.ElapsedMilliseconds + " ms");
                Console.WriteLine();
                Console.WriteLine(result);
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