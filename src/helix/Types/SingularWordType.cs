using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Primitives;
using Helix.TypeChecking;

namespace Helix.Types;

public record SingularWordType : HelixType {
    public long Value { get; }

    public SingularWordType(long value) {
        this.Value = value;
    }

    public override PassingSemantics GetSemantics(TypeFrame types) {
        return PassingSemantics.ValueType;
    }

    public override bool IsWord(TypeFrame types) => true;

    public override Option<ITypedExpression> ToSyntax(TokenLocation loc, TypeFrame types) {
        return new WordLiteral {
            Location = loc,
            Value = this.Value
        };
    }       

    public override string ToString() => this.Value.ToString();

    public override HelixType GetSupertype(TypeFrame types) => PrimitiveType.Word;
}