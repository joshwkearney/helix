using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax {
    public interface IParseTree {
        public TokenLocation Location { get; }
        
        public bool IsPure { get; }

        public Option<HelixType> AsType(TypeFrame types) => Option.None;

        public TypeCheckResult CheckTypes(TypeFrame types);
    }
}
