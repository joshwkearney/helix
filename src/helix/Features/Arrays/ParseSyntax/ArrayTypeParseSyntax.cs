using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Parsing;

namespace Helix.Features.Arrays {
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

        public ISyntax CheckTypes(TypeFrame types) {
            throw new InvalidOperationException();
        }
    }
}
