using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Arrays.Sytnax {
    public record ArrayIndexSyntax : ISyntax {
        public required ArrayType ArraySignature { get; init; }
        
        public required ISyntax Operand { get; init; }
        
        public required ISyntax Index { get; init; }
        
        public required bool AlwaysJumps { get; init; }
        
        public TokenLocation Location => this.Operand.Location;

        public HelixType ReturnType => this.ArraySignature.InnerType;

        public ILValue ToLValue(TypeFrame types) {
            return new ILValue.ArrayIndex(this.Operand, this.Index, new PointerType(this.ArraySignature.InnerType));
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var target = this.Operand.GenerateCode(types, writer);
            
            return new CPointerDereference {
                Target = new CBinaryExpression {
                    Left = new CMemberAccess {
                        Target = target,
                        MemberName = "data"
                    },
                    Right = this.Index.GenerateCode(types, writer),
                    Operation = BinaryOperationKind.Add
                }
            };
        }
    }
}
