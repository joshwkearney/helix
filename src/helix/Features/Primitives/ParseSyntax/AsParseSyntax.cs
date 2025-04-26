using Helix.Analysis.TypeChecking;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Primitives.ParseSyntax {
    public record AsParseSyntax : IParseSyntax {
        public required TokenLocation Location { get; init; }
        
        public required IParseSyntax Operand { get; init; }

        public required IParseSyntax TypeSyntax { get; init; }

        public bool IsPure => this.Operand.IsPure && this.TypeSyntax.IsPure;

        public TypeCheckResult CheckTypes(TypeFrame types) {
            (var arg, types) = this.Operand.CheckTypes(types);

            if (!this.TypeSyntax.AsType(types).TryGetValue(out var targetType)) {
                throw TypeException.ExpectedTypeExpression(this.TypeSyntax.Location);
            }

            arg = arg.UnifyTo(targetType, types);
            
            return new TypeCheckResult(arg, types);
        }
    }
}