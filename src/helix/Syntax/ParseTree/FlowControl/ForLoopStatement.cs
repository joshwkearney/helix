using Helix.Parsing;
using Helix.Syntax.ParseTree.Primitives;
using Helix.Syntax.ParseTree.Variables;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.FlowControl;
using Helix.Syntax.TypedTree.Primitives;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.FlowControl;

public class ForLoopStatement : IParseStatement {
    public required TokenLocation Location { get; init; }
    
    public required Token VariableToken { get; init; }
    
    public required IParseExpression StartValue { get; init; }
    
    public required IParseExpression EndValue { get; init; }
    
    public required bool IsInclusive { get; init; }
    
    public required IParseStatement Body { get; init; }
    
    public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types) {
        var counterName = this.VariableToken.Value;

        var startIndex = new BinaryExpression {
            Location = this.StartValue.Location,
            Left = this.StartValue,
            Right = new WordLiteral {
                Location = this.StartValue.Location,
                Value = 1
            },
            Operator = BinaryOperationKind.Subtract
        };

        var counterStatement = new VariableStatement {
            Location = this.VariableToken.Location,
            VariableName = counterName,
            VariableType = new TypeExpression() {
                Location = this.StartValue.Location,
                Type = PrimitiveType.Word
            },
            Assignment = startIndex
        };
        
        var counterAccess = new VariableAccessExpression {
            Location = this.StartValue.Location,
            VariableName = counterName
        };

        var counterIncrement = new AssignmentStatement {
            Location = this.StartValue.Location,
            Left = counterAccess,
            Right = new BinaryExpression {
                Location = this.StartValue.Location,
                Left = counterAccess,
                Right = new WordLiteral {
                    Location = this.StartValue.Location,
                    Value = 1
                },
                Operator = BinaryOperationKind.Add
            }
        };
        
        var breakTest = new IfStatement {
            Location = this.Location,
            Condition = new BinaryExpression {
                Location = this.Location,
                Left = counterAccess,
                Right = this.EndValue,
                Operator = this.IsInclusive
                    ? BinaryOperationKind.GreaterThan
                    : BinaryOperationKind.GreaterThanOrEqualTo
            },
            Affirmative = new LoopControlStatement {
                Location = this.Location,
                Kind = LoopControlKind.Break
            },
            Negative = new BlockStatement {
                Location = this.Location,
                Statements = []
            }
        };
        
        var result = new BlockStatement {
            Location = this.Location,
            Statements = [
                counterStatement,
                new LoopStatement {
                    Location = this.Location,
                    Body = new BlockStatement {
                        Location = this.Body.Location,
                        Statements = [
                            counterIncrement,
                            breakTest,
                            this.Body
                        ]
                    }
                }
            ]
        };

        return result.CheckTypes(types);
    }
}