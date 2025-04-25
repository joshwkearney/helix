using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Parsing;

namespace Helix.Syntax {
    public interface IParseSyntax {
        public TokenLocation Location { get; }
        
        public bool IsPure { get; }

        public Option<HelixType> AsType(TypeFrame types) => Option.None;

        public TypeCheckResult CheckTypes(TypeFrame types);
    }
}
