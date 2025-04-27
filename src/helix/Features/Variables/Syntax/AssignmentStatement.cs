using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Variables.Syntax {
    public record AssignmentStatement : ISyntax {
        public required TokenLocation Location { get; init; }
        
        public required ILValue Left { get; init; }
        
        public required ISyntax Right { get; init; }
        
        public required bool AlwaysJumps { get; init; }
        
        public HelixType ReturnType => PrimitiveType.Void;
        
        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var target = this.GenerateLValue(this.Left, types, writer);
            var assign = this.Right.GenerateCode(types, writer);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Assignment statement");

            writer.WriteStatement(new CAssignment() {
                Left = target,
                Right = assign
            });

            writer.WriteEmptyLine();
            return new CIntLiteral(0);
        }

        private ICSyntax GenerateLValue(ILValue lvalue, TypeFrame types, ICStatementWriter writer) {
            if (lvalue is ILValue.Local local) {
                return new CVariableLiteral(writer.GetVariableName(local.VariablePath));
            }
            else if (lvalue is ILValue.Dereference deref) {
                var inner = deref.Operand.GenerateCode(types, writer);

                return new CPointerDereference {
                    Target = inner
                };
            }
            else if (lvalue is ILValue.ArrayIndex array) {
                var inner = array.Operand.GenerateCode(types, writer);
                var index = array.Index.GenerateCode(types, writer);

                return new CPointerDereference {
                    Target = new CBinaryExpression {
                        Left = inner,
                        Right = index,
                        Operation = BinaryOperationKind.Add
                    }
                };
            }
            else if (lvalue is ILValue.StructMemberAccess mem) {
                var inner = this.GenerateLValue(mem.Parent, types, writer);

                return new CMemberAccess {
                    Target = inner,
                    MemberName = mem.MemberName,
                    IsPointerAccess = false
                };
            }
            else {
                throw new InvalidOperationException("LValue type not supported");
            }
        }
    }
}