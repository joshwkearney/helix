using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Arrays {
    public record ArrayTypeExpression : IParseExpression {
        public required TokenLocation Location { get; init; }
        
        public required IParseExpression Operand { get; init; }
        
        Option<HelixType> IParseExpression.AsType(TypeFrame types) {
            return this.Operand
                .AsType(types)
                .Select(x => new Types.ArrayType(x))
                .Select(x => (HelixType)x);
        }

        public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) {
            throw new InvalidOperationException();
        }
    }
}
