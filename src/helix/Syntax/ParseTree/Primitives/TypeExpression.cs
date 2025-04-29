using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Primitives;

public class TypeExpression : IParseExpression {
    public required TokenLocation Location { get; init; }
    
    public required HelixType Type { get; init; }
    
    public bool IsPure => true;

    public Option<HelixType> AsType(TypeFrame types) => this.Type;
    
    public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) {
        throw new InvalidOperationException();
    }
}