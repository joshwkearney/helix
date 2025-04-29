using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax {
    public interface IParseTree {
        public TokenLocation Location { get; }
        
        public Option<HelixType> AsType(TypeFrame types) => Option.None;

        public TypeCheckResult<ITypedTree> CheckTypes(TypeFrame types);
    }
    
    public interface IParseStatement {
        public TokenLocation Location { get; }
        
        public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types);
    }
}
