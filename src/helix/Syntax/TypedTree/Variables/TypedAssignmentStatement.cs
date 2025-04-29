using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;

namespace Helix.Syntax.TypedTree.Variables {
    public record TypedAssignmentStatement : ITypedStatement {
        public required TokenLocation Location { get; init; }
        
        public required ILValue Left { get; init; }
        
        public required ITypedExpression Right { get; init; }

        public bool AlwaysJumps => false;
        
        public void GenerateIR(IRWriter writer, IRFrame context) {
            if (this.Left is ILValue.Local local) {
                var assign = this.Right.GenerateIR(writer, context);
                
                if (context.AllocatedVariables.Contains(local.VariablePath)) {
                    // We need to store into this variable as a reference   
                    writer.CurrentBlock.Add(new StoreReferenceOp {
                        Reference = context.GetVariable(local.VariablePath),
                        Value = assign
                    });
                }
                else {
                    // Emit a temporary assignment that will be removed in the SSA pass
                    writer.CurrentBlock.Add(new AssignLocalOp {
                        LocalName = context.GetVariable(local.VariablePath),
                        Value = assign
                    });
                }
            }
            else if (this.Left is ILValue.Dereference deref) {
                // We need to store into the reference given by the lvalue
                var reference = deref.Operand.GenerateIR(writer, context);
                var assign = this.Right.GenerateIR(writer, context);
                
                writer.CurrentBlock.Add(new StoreReferenceOp {
                    Reference = reference,
                    Value = assign
                });
            }
            else if (this.Left is ILValue.ArrayIndex arrayIndex) {
                // We need to store into an array
                var array = arrayIndex.Operand.GenerateIR(writer, context);
                var index = arrayIndex.Index.GenerateIR(writer, context);
                var assign = this.Right.GenerateIR(writer, context);
                
                writer.CurrentBlock.Add(new StoreArrayOp {
                    Array = array,
                    Index = index,
                    Value = assign
                });
            }
            else if (this.Left is ILValue.StructMemberAccess structAccess) {
                throw new NotImplementedException();
            }
            else {
                throw new InvalidOperationException();
            }
        }
        
        public void GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var target = this.GenerateLValue(this.Left, types, writer);
            var assign = this.Right.GenerateCode(types, writer);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Assignment statement");

            writer.WriteStatement(new CAssignment {
                Left = target,
                Right = assign
            });

            writer.WriteEmptyLine();
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