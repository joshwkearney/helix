using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Primitives;

public class TypeParseTree : IParseTree {
    public required TokenLocation Location { get; init; }
    
    public required HelixType Type { get; init; }
    
    public bool IsPure => true;

    public Option<HelixType> AsType(TypeFrame types) => this.Type;
    
    public TypeCheckResult<ITypedTree> CheckTypes(TypeFrame types) {
        throw new InvalidOperationException();
    }
}