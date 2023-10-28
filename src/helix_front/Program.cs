using Helix;
using Helix.Analysis.Predicates;

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


public class DummyTerm : ISyntaxPredicate {
    public string Name { get; init; }

    public bool IsNegated { get; init; }

    public DummyTerm(string name) {
        this.Name = name;
    }

    public override ISyntaxPredicate Negate() {
        return new DummyTerm(this.Name) {
            IsNegated = !this.IsNegated
        };
    }

    public override bool Equals(ISyntaxPredicate other) {
        if (other is DummyTerm dummy) {
            return dummy.Name == this.Name && dummy.IsNegated == this.IsNegated;
        }

        return false;
    }

    public override bool Equals(object other) {
        if (other is DummyTerm dummy) {
            return dummy.Name == this.Name && dummy.IsNegated == this.IsNegated;
        }

        return false;
    }

    public override int GetHashCode() => this.Name.GetHashCode() * 11 * this.IsNegated.GetHashCode();

    public override string ToString() {
        return (this.IsNegated ? "!" : "") + this.Name;
    }
}