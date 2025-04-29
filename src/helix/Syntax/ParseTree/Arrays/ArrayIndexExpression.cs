using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Arrays;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Arrays {
    public record ArrayIndexExpression : IParseExpression {
        public required TokenLocation Location { get; init; }
        
        public required IParseExpression Operand { get; init; }
        
        public required IParseExpression Index { get; init; }
        
        public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) {
            (var operand, types) = this.Operand.CheckTypes(types);
            (var index, types) = this.Index.CheckTypes(types);
                
            index = index.UnifyTo(PrimitiveType.Word, types);

            // Make sure we have an array
            if (operand.ReturnType is not Types.ArrayType arrayType) {
                throw TypeException.ExpectedArrayType(
                    this.Operand.Location, 
                    operand.ReturnType);
            }
            
            var result = new TypedArrayIndexExpression {
                ArraySignature = arrayType,
                Operand = operand,
                Index = index
            };

            return new TypeCheckResult<ITypedExpression>(result, types);
        }
    }
}