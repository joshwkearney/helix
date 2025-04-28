using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Variables {
    public record VariableStatement : ITypedTree {
        public required TokenLocation Location { get; init; }

        public required ITypedTree Assignment { get; init; }
        
        public required IdentifierPath Path { get; init; }
        
        public required PointerType VariableSignature { get; init; }
        
        public required bool AlwaysJumps { get; init; }
        
        public HelixType ReturnType => PrimitiveType.Void;

        public Option<HelixType> AsType(TypeFrame types) {
            return new NominalType(this.Path, NominalTypeKind.Variable);
        }

        public Immediate GenerateIR(IRWriter writer, IRFrame context) {
            if (context.AllocatedVariables.Contains(this.Path)) {
                var name = writer.GetName(this.Path.Segments.Last());
                writer.CurrentBlock.Add(new AllocateOp {
                    ReturnType = this.VariableSignature,
                    ReturnValue = name
                });
                
                var assign = this.Assignment.GenerateIR(writer, context);
                writer.CurrentBlock.Add(new StoreReferenceOp {
                    Reference = name,
                    Value = assign
                });

                context.SetVariable(this.Path, name);
                return new Immediate.Void();
            }
            else {
                var name = writer.GetName(this.Path.Segments.Last());
                var assign = this.Assignment.GenerateIR(writer, context);
                
                writer.CurrentBlock.Add(new CreateLocalOp {
                    ReturnType = this.Assignment.ReturnType,
                    LocalName = name
                });
                
                writer.CurrentBlock.Add(new AssignLocalOp {
                    LocalName = name,
                    Value = assign
                });
                
                context.SetVariable(this.Path, name);
                return new Immediate.Void();
            }
        }
        
        public ICSyntax GenerateCode(TypeFrame flow, ICStatementWriter writer) {
            var assign = this.Assignment.GenerateCode(flow, writer);
            
            this.GenerateStackAllocation(assign, flow, writer);
            return new CIntLiteral(0);
        }

        private void GenerateStackAllocation(ICSyntax assign, TypeFrame types, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.Path);
            var assignType = this.Assignment.ReturnType;
            var cReturnType = writer.ConvertType(assignType, types);

            var stat = new CVariableDeclaration() {
                Type = cReturnType,
                Name = name,
                Assignment = Option.Some(assign)
            };

            writer.WriteStatement(stat);
            writer.WriteEmptyLine();
            writer.VariableKinds[this.Path] = CVariableKind.Local;
        }

        private void GenerateRegionAllocation(ICSyntax assign, ICSyntax allocLifetime,
                                              TypeFrame types, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.Path);
            var assignType = this.Assignment.ReturnType;
            var cReturnType = writer.ConvertType(assignType, types);

            writer.WriteStatement(new CVariableDeclaration() {
                Name = name,
                Type = new CPointerType(cReturnType),
                Assignment = new CRegionAllocExpression() {
                    Type = cReturnType,
                    Lifetime = allocLifetime
                }
            });

            var assignmentDecl = new CAssignment() {
                Left = new CPointerDereference() {
                    Target = new CVariableLiteral(name)
                },
                Right = assign
            };

            writer.WriteStatement(assignmentDecl);
            writer.WriteEmptyLine();
            writer.VariableKinds[this.Path] = CVariableKind.Allocated;
        }
    }
}