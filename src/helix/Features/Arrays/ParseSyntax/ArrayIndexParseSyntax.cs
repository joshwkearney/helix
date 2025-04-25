using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Features.Variables;


namespace Helix.Features.Arrays {
    public record ArrayIndexParseSyntax : IParseSyntax {
        public required TokenLocation Location { get; init; }
        
        public required IParseSyntax Operand { get; init; }
        
        public required IParseSyntax Index { get; init; }

        public bool IsPure => this.Operand.IsPure && this.Index.IsPure;
        
        public TypeCheckResult CheckTypes(TypeFrame types) {
            (var target, types) = this.Operand.CheckTypes(types);
            (var index, types) = this.Index.CheckTypes(types);
                
            index = index.UnifyTo(PrimitiveType.Word, types);

            // Make sure we have an array
            if (target.ReturnType is not ArrayType arrayType) {
                throw TypeException.ExpectedArrayType(
                    this.Operand.Location, 
                    target.ReturnType);
            }

            var adapter = new ArrayToPointerSyntax {
                ArraySignature = arrayType,
                Operand = target,
                Index = index
            };

            var result = new DereferenceSyntax {
                Location = adapter.Location,
                Operand = adapter,
                OperandSignature = new PointerType(arrayType.InnerType),
                IsLValue = false
            };

            return new TypeCheckResult(result, types);
        }
    }
}