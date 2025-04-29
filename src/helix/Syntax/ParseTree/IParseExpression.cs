using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree {
    public interface IParseExpression {
        public TokenLocation Location { get; }
        
        public Option<HelixType> AsType(TypeFrame types) => Option.None;

        public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types);
    }
}
