using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Arrays.ParseSyntax {
    public record ArrayTypeParseSyntax : IParseSyntax {
        public required TokenLocation Location { get; init; }
        
        public required IParseSyntax Operand { get; init; }

        public bool IsPure => this.Operand.IsPure;

        Option<HelixType> IParseSyntax.AsType(TypeFrame types) {
            return this.Operand
                .AsType(types)
                .Select(x => new ArrayType(x))
                .Select(x => (HelixType)x);
        }

        public TypeCheckResult CheckTypes(TypeFrame types) {
            throw new InvalidOperationException();
        }
    }
}
