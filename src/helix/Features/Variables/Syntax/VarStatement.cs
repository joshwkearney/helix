using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.IRGeneration;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Variables.Syntax {
    public record VarStatement : ISyntax {
        public required TokenLocation Location { get; init; }

        public required ISyntax Assignment { get; init; }
        
        public required IdentifierPath Path { get; init; }
        
        public required bool AlwaysJumps { get; init; }
        
        public HelixType ReturnType => PrimitiveType.Void;

        public Option<HelixType> AsType(TypeFrame types) {
            return new NominalType(this.Path, NominalTypeKind.Variable);
        }

        public Immediate GenerateIR(IRWriter writer, IRFrame context, Immediate? returnName = null) {
            var name = writer.GetVariable(this.Path.Segments.Last());
            context.SetVariable(this.Path, name);
            
            return this.Assignment.GenerateIR(writer, context, name);
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