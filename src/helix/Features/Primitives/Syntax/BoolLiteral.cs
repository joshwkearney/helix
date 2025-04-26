using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Primitives.Syntax {
    public record BoolLiteral : IParseSyntax, ISyntax {
        public required TokenLocation Location { get; init; }
        
        public required bool Value { get; init; }

        public bool AlwaysJumps => false;

        public HelixType ReturnType => new SingularBoolType(this.Value);
        
        public bool IsPure => true;
        
        public Option<HelixType> AsType(TypeFrame types) {
            return new SingularBoolType(this.Value);
        }

        public TypeCheckResult CheckTypes(TypeFrame types) => new(this, types);

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CIntLiteral(this.Value ? 1 : 0);
        }
    }
}
