using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features.Arrays {
    public record ArrayToPointerSyntax : ISyntax {
        public required ArrayType ArraySignature { get; init; }
        
        public required ISyntax Operand { get; init; }
        
        public required ISyntax Index { get; init; }
        
        public required bool AlwaysJumps { get; init; }
        
        public TokenLocation Location => this.Operand.Location;

        public HelixType ReturnType => new PointerType(this.ArraySignature.InnerType);

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var target = this.Operand.GenerateCode(types, writer);

            ICSyntax newData = new CMemberAccess() {
                Target = target,
                MemberName = "data"
            };

            if (this.Index != null) {
                newData = new CBinaryExpression() {
                    Left = newData,
                    Right = this.Index.GenerateCode(types, writer),
                    Operation = BinaryOperationKind.Add
                };
            }

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Array to pointer conversion");

            var ptrType = writer.ConvertType(new PointerType(this.ArraySignature.InnerType), types);
            var ptrValue = new CCompoundExpression() {
                Arguments = new[] {
                    newData,
                    new CMemberAccess() {
                        Target = target,
                        MemberName = "region"
                    }
                },
                Type = ptrType
            };

            var result = writer.WriteImpureExpression(ptrType, ptrValue);
            writer.WriteEmptyLine();

            return result;
        }
    }
}
