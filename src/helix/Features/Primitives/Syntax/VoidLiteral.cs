using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Features.Primitives {
    public record VoidLiteral : IParseSyntax, ISyntax {
        public required TokenLocation Location { get; init; }

        public bool AlwaysJumps => false;

        public HelixType ReturnType => PrimitiveType.Void;

        public bool IsPure => true;

        public Option<HelixType> AsType(TypeFrame types) => PrimitiveType.Void;

        public TypeCheckResult CheckTypes(TypeFrame types) => new(this, types);

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CIntLiteral(0);
        }
    }
}
