using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.TypeChecking;

namespace Helix.Syntax.ParseTree;

public interface IParseStatement {
    public TokenLocation Location { get; }
        
    public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types);
}