using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Arrays {
    public record ArrayIndexTypedTree : ITypedTree {
        public required ArrayType ArraySignature { get; init; }
        
        public required ITypedTree Operand { get; init; }
        
        public required ITypedTree Index { get; init; }
        
        public TokenLocation Location => this.Operand.Location;

        public HelixType ReturnType => this.ArraySignature.InnerType;

        public ILValue ToLValue(TypeFrame types) {
            return new ILValue.ArrayIndex(this.Operand, this.Index, new PointerType(this.ArraySignature.InnerType));
        }
        
        public Immediate GenerateIR(IRWriter writer, IRFrame context) {
            var array = this.Operand.GenerateIR(writer, context);
            var index = this.Index.GenerateIR(writer, context);
            var temp = writer.GetName();
            
            writer.CurrentBlock.Add(new LoadArrayOp {
                Array = array,
                Index = index,
                ReturnValue = temp,
                ReturnType = this.ReturnType
            });
            
            return temp;
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
