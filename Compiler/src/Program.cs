using System;
using System.Diagnostics;
using System.IO;
using Attempt20.Experimental;

namespace Attempt20 {
    public class Program {
        public static void Main(string[] args) {
            var file = File.ReadAllText("resources/Program.txt");

            try {
                /*var expr1 = BoolSyntax.Not(
                    BoolSyntax.Or(
                        BoolSyntax.Atom(new UnionIsAtom(true, "x", "some")),
                        BoolSyntax.Atom(new UnionIsAtom(true, "x", "other")))
                );

                var expr2 = BoolSyntax.Not(
                    BoolSyntax.Or(
                        BoolSyntax.Atom(new ComparisonAtom(
                            ComparisonArgument.String("x"),
                            ComparisonArgument.Constant(5),
                            ComparisonKind.LessThan
                        )),
                        BoolSyntax.Atom(new ComparisonAtom(
                            ComparisonArgument.Constant(10), 
                            ComparisonArgument.String("x"),
                            ComparisonKind.LessThan
                        )),
                         BoolSyntax.And(
                        BoolSyntax.Atom(new ComparisonAtom(
                            ComparisonArgument.Constant(10),
                            ComparisonArgument.String("x"),
                            ComparisonKind.LessThan
                        )),
                        BoolSyntax.Atom(new ComparisonAtom(
                            ComparisonArgument.Constant(10),
                            ComparisonArgument.String("x"),
                            ComparisonKind.LessThan
                        ))
                    )
                   
                ));

                System.Console.WriteLine(expr2);
                System.Console.WriteLine(expr2.ToCNF().Simplify());*/

                var watch = new Stopwatch();

                watch.Start();
                var c = new TrophyCompiler(file).Compile();
                var time = watch.ElapsedMilliseconds;

                Console.WriteLine("Compiled 'Program.txt' in " + time + " ms");
                Console.WriteLine();
                Console.WriteLine(c);
                File.WriteAllText("resources/Output.txt", c);
            }
            catch (TrophyException ex) {
                Console.WriteLine(ex.CreateConsoleMessage(file));
            }
        }
    }
}