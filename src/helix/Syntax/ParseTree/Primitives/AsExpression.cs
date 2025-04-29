using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.TypeChecking;

namespace Helix.Syntax.ParseTree.Primitives {
    public record AsExpression : IParseExpression {
        public required TokenLocation Location { get; init; }
        
        public required IParseExpression Operand { get; init; }

        public required IParseExpression TypeExpression { get; init; }
        
        public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) {
            (var arg, types) = this.Operand.CheckTypes(types);

            if (!this.TypeExpression.AsType(types).TryGetValue(out var targetType)) {
                throw TypeException.ExpectedTypeExpression(this.TypeExpression.Location);
            }
            
            arg = arg.UnifyTo(targetType, types);
            
            return new TypeCheckResult<ITypedExpression>(arg, types);
        }
    }
}