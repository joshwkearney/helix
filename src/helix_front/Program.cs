using Helix;
using Helix.Analysis.Flow;
using Helix.Analysis.Predicates;
using Helix.Analysis.TypeChecking;
using Helix.Parsing;
using Helix.Syntax;

var header = File.ReadAllText("../../../../../Resources/Helix.h");
var contents = File.ReadAllText("../../../../../Resources/Program.helix");

//var A = new DummyTerm("A");
//var B = new DummyTerm("B");

//var result = A.Or(B).And(A).Negate().Or(B);

//Console.WriteLine(result);

try {
    var parse = new HelixCompiler(header, contents).Compile();

    File.WriteAllText("../../../../../Resources/Program.c", parse);
    Console.WriteLine(parse);
}
catch (HelixException ex) {
    Console.WriteLine(ex.Message);
}

Console.Read();