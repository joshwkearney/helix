using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.Unions;

namespace Helix.Features.Primitives {
    public record AsParseSyntax : IParseSyntax {
        public required TokenLocation Location { get; init; }
        
        public required IParseSyntax Operand { get; init; }

        public required IParseSyntax TypeSyntax { get; init; }

        public bool IsPure => this.Operand.IsPure && this.TypeSyntax.IsPure;

        public ISyntax CheckTypes(TypeFrame types) {
            var arg = this.Operand.CheckTypes(types).ToRValue(types);

            if (!this.TypeSyntax.AsType(types).TryGetValue(out var targetType)) {
                throw TypeException.ExpectedTypeExpression(this.TypeSyntax.Location);
            }

            arg = arg.UnifyTo(targetType, types);
            return arg;
        }
    }
}