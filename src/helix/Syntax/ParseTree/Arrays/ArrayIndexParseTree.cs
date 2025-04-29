using Helix.Parsing;
using Helix.Syntax.TypedTree.Arrays;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Arrays {
    public record ArrayIndexParseTree : IParseTree {
        public required TokenLocation Location { get; init; }
        
        public required IParseTree Operand { get; init; }
        
        public required IParseTree Index { get; init; }
        
        public TypeCheckResult<ITypedTree> CheckTypes(TypeFrame types) {
            (var operand, types) = this.Operand.CheckTypes(types);
            (var index, types) = this.Index.CheckTypes(types);
                
            index = index.UnifyTo(PrimitiveType.Word, types);

            // Make sure we have an array
            if (operand.ReturnType is not ArrayType arrayType) {
                throw TypeException.ExpectedArrayType(
                    this.Operand.Location, 
                    operand.ReturnType);
            }
            
            var result = new ArrayIndexTypedTree {
                ArraySignature = arrayType,
                Operand = operand,
                Index = index
            };

            return new TypeCheckResult<ITypedTree>(result, types);
        }
    }
}