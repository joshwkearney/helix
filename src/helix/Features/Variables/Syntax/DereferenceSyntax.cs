using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
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
        
        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var target = this.Operand.GenerateCode(types, writer);

            // if (this.IsLValue) {
            //     return new CMemberAccess() {
            //         Target = target,
            //         MemberName = "data"
            //     };
            // }
            // else {
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
                //writer.VariableKinds[this.tempPath] = CVariableKind.Local;

                return new CVariableLiteral(tempName);
            //}
        }
    }
}
