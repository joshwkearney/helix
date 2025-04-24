using Helix.Analysis.Predicates;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Features.Primitives {
    public record BoolLiteral : IParseSyntax, ISyntax {
        public required TokenLocation Location { get; init; }
        
        public required bool Value { get; init; }
        
        public ISyntaxPredicate Predicate => ISyntaxPredicate.Empty;
        
        public HelixType ReturnType => new SingularBoolType(this.Value);
        
        public bool IsPure => true;
        
        public Option<HelixType> AsType(TypeFrame types) {
            return new SingularBoolType(this.Value);
        }

        public ISyntax CheckTypes(TypeFrame types) => this;

        public IParseSyntax ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CIntLiteral(this.Value ? 1 : 0);
        }
    }
}
