using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Arrays.Sytnax;
using Helix.Features.Variables.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Arrays.ParseSyntax {
    public record ArrayIndexParseSyntax : IParseSyntax {
        public required TokenLocation Location { get; init; }
        
        public required IParseSyntax Operand { get; init; }
        
        public required IParseSyntax Index { get; init; }

        public bool IsPure => this.Operand.IsPure && this.Index.IsPure;
        
        public TypeCheckResult CheckTypes(TypeFrame types) {
            (var operand, types) = this.Operand.CheckTypes(types);
            (var index, types) = this.Index.CheckTypes(types);
                
            index = index.UnifyTo(PrimitiveType.Word, types);

            // Make sure we have an array
            if (operand.ReturnType is not ArrayType arrayType) {
                throw TypeException.ExpectedArrayType(
                    this.Operand.Location, 
                    operand.ReturnType);
            }
            
            var result = new ArrayIndexSyntax {
                ArraySignature = arrayType,
                Operand = operand,
                Index = index,
                AlwaysJumps = operand.AlwaysJumps && index.AlwaysJumps
            };

            return new TypeCheckResult(result, types);
        }
    }
}