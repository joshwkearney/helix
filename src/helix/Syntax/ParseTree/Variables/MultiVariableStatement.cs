using Helix.Parsing;
using Helix.Syntax.ParseTree.FlowControl;
using Helix.Syntax.ParseTree.Structs;
using Helix.Syntax.TypedTree;
using Helix.TypeChecking;

namespace Helix.Syntax.ParseTree.Variables;

public record MultiVariableStatement : IParseStatement {
    public required TokenLocation Location { get; init; }
        
    public required IReadOnlyList<string> VariableNames { get; init; }
        
    public required IReadOnlyList<Option<IParseExpression>> VariableTypes { get; init; }
        
    public required IParseExpression Assignment { get; init; }

    public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types) {
        (var assign, types) = this.Assignment.CheckTypes(types);
        
        if (!assign.ReturnType.AsStruct(types).TryGetValue(out var sig)) {
            throw new TypeException(
                this.Location,
                "Invalid Desconstruction",
                $"Cannot deconstruct non-struct type '{assign.ReturnType}'");
        }

        if (sig.Members.Count != this.VariableNames.Count) {
            throw new TypeException(
                this.Location,
                "Invalid Desconstruction",
                "The number of variables provided does not match "
                + $"the number of members on struct type '{assign.ReturnType}'");
        }

        var tempName = types.GetVariableName();

        var tempStat = new VariableStatement {
            Location = this.Location,
            VariableName = tempName,
            VariableType = Option.None,
            Assignment = this.Assignment
        };

        var stats = new List<IParseStatement> { tempStat };

        for (int i = 0; i < sig.Members.Count; i++) {
            var memAssign = new VariableStatement {
                Location = this.Location,
                VariableName = this.VariableNames[i],
                VariableType = this.VariableTypes[i],
                Assignment = new MemberAccessExpression {
                    Location = this.Location,
                    MemberName = sig.Members[i].Name,
                    Operand = new VariableAccessExpression {
                        Location = this.Location,
                        VariableName = tempName
                    }
                }
            };

            stats.Add(memAssign);
        }

        var result = new BlockStatement {
            Location = this.Location,
            Statements = stats
        };

        return result.CheckTypes(types);
    }
}