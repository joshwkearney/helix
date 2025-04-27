using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.IRGeneration;
using Helix.Parsing;
using Helix.Parsing.IR;
using Helix.Syntax;

namespace Helix.Features.Variables.Syntax {
    public record DereferenceSyntax : ISyntax {
        public required TokenLocation Location { get; init; }

        public required ISyntax Operand { get; init; }
        
        public required PointerType OperandSignature { get; init; }
        
        public required bool AlwaysJumps { get; init; }

        public HelixType ReturnType => this.OperandSignature.InnerType;

        public ILValue ToLValue(TypeFrame types) {
            return new ILValue.Dereference(this.Operand);
        }
        
        public Immediate GenerateIR(IRWriter writer, IRFrame context) {
            var temp = writer.GetName();
        
            writer.WriteOp(new LoadReferenceOp {
                Operand = this.Operand.GenerateIR(writer, context),
                ReturnType = this.ReturnType,
                ReturnValue = temp
            });

            return temp;
        }
        
        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var target = this.Operand.GenerateCode(types, writer);
            var pointerType = this.Operand.AssertIsPointer(types);
            var tempName = writer.GetVariableName();
            var tempType = writer.ConvertType(pointerType.InnerType, types);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Pointer dereference");
            
            writer.WriteStatement(new CVariableDeclaration() {
                Name = tempName,
                Type = tempType,
                Assignment = new CPointerDereference() {
                    Target = new CMemberAccess() {
                        Target = target,
                        MemberName = "data",
                        IsPointerAccess = false
                    }
                }
            });

            writer.WriteEmptyLine();

            return new CVariableLiteral(tempName);
        }
    }
}
