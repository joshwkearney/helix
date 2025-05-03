using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Variables;

public record TypedVariableStatement : ITypedStatement {
    public required TokenLocation Location { get; init; }

    public required ITypedExpression Assignment { get; init; }
        
    public required IdentifierPath Path { get; init; }
        
    public bool AlwaysJumps => false;

    public void GenerateIR(IRWriter writer, IRFrame context) {
        if (context.AllocatedVariables.Contains(this.Path)) {
            var name = writer.GetName(this.Path.Segments.Last());
            writer.CurrentBlock.Add(new AllocateReferenceInstruction {
                InnerType = this.Assignment.ReturnType,
                ReturnValue = name
            });
                
            var assign = this.Assignment.GenerateIR(writer, context);
            writer.CurrentBlock.Add(new StoreInstruction {
                Reference = name,
                Value = assign
            });

            context.SetVariable(this.Path, name);
        }
        else {
            var name = writer.GetName(this.Path.Segments.Last());
            var assign = this.Assignment.GenerateIR(writer, context);
                
            writer.CurrentBlock.Add(new CreateLocalInstruction {
                ReturnType = this.Assignment.ReturnType,
                LocalName = name
            });
                
            writer.CurrentBlock.Add(new AssignLocalInstruction {
                LocalName = name,
                Value = assign
            });
                
            context.SetVariable(this.Path, name);
        }
    }
        
    public void GenerateCode(TypeFrame flow, ICStatementWriter writer) {
        var assign = this.Assignment.GenerateCode(flow, writer);
            
        this.GenerateStackAllocation(assign, flow, writer);
    }

    private void GenerateStackAllocation(ICSyntax assign, TypeFrame types, ICStatementWriter writer) {
        var name = writer.GetVariableName(this.Path);
        var assignType = this.Assignment.ReturnType;
        var cReturnType = writer.ConvertType(assignType, types);

        var stat = new CVariableDeclaration {
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

        writer.WriteStatement(new CVariableDeclaration {
            Name = name,
            Type = new CPointerType(cReturnType),
            Assignment = new CRegionAllocExpression {
                Type = cReturnType,
                Lifetime = allocLifetime
            }
        });

        var assignmentDecl = new CAssignment {
            Left = new CPointerDereference {
                Target = new CVariableLiteral(name)
            },
            Right = assign
        };

        writer.WriteStatement(assignmentDecl);
        writer.WriteEmptyLine();
        writer.VariableKinds[this.Path] = CVariableKind.Allocated;
    }
}