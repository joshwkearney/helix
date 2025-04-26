using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Primitives.ParseSyntax;

public class TypeParseSyntax : IParseSyntax {
    public required TokenLocation Location { get; init; }
    
    public required HelixType Type { get; init; }
    
    public bool IsPure => true;

    public Option<HelixType> AsType(TypeFrame types) => this.Type;
    
    public TypeCheckResult CheckTypes(TypeFrame types) {
        throw new InvalidOperationException();
    }
}