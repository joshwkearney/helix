using Helix.Analysis.Predicates;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Features.Primitives {
    public record WordLiteral : IParseSyntax, ISyntax {
        public required TokenLocation Location { get; init; }
        
        public required long Value { get; init; }

        public HelixType ReturnType => new SingularWordType(this.Value);

        public ISyntaxPredicate Predicate => ISyntaxPredicate.Empty;
        
        public bool IsPure => true;

        public Option<HelixType> AsType(TypeFrame types) {
            return new SingularWordType(this.Value);
        }

        public ISyntax CheckTypes(TypeFrame types) => this;

        public IParseSyntax ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CIntLiteral(this.Value);
        }
    }
}
