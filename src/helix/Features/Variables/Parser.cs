using Helix.Features.Primitives;
using Helix.Features.Primitives.ParseSyntax;
using Helix.Features.Variables.ParseSyntax;
using Helix.Syntax;

namespace Helix.Parsing;

public partial class Parser {
    private IParseSyntax VarExpression() {
        var startLok = this.Advance(TokenKind.VarKeyword).Location;
        var names = new List<string>();
        var types = new List<Option<IParseSyntax>>();

        while (true) {
            var name = this.Advance(TokenKind.Identifier).Value;
            names.Add(name);

            if (this.TryAdvance(TokenKind.AsKeyword)) {
                types.Add(Option.Some<IParseSyntax>(this.TopExpression()));
            }
            else {
                types.Add(Option.None);
            }

            if (this.TryAdvance(TokenKind.Assignment)) {
                break;
            }
            else {
                this.Advance(TokenKind.Comma);
            }
        }

        var assign = this.TopExpression();
        var loc = startLok.Span(assign.Location);

        return new VarParseStatement {
            Location = loc,
            VariableNames = names,
            VariableTypes = types,
            Assignment = assign
        };
    }
    
    private IParseSyntax VariableAccess() {
        var tok = this.Advance(TokenKind.Identifier);

        return new VariableAccessParseSyntax {
            Location = tok.Location,
            VariableName = tok.Value
        };
    }
    
    private IParseSyntax AssignmentStatement() {
        var start = this.TopExpression();

        if (this.TryAdvance(TokenKind.Assignment)) {
            var assign = this.TopExpression();
            var loc = start.Location.Span(assign.Location);

            var result = new AssignmentParseStatement {
                Location = loc,
                Left = start,
                Right = assign
            };

            return result;
        }
        else {
            BinaryOperationKind op;

            // These are operators like += and -=
            if (this.TryAdvance(TokenKind.PlusAssignment)) {
                op = BinaryOperationKind.Add;
            }
            else if (this.TryAdvance(TokenKind.MinusAssignment)) {
                op = BinaryOperationKind.Subtract;
            }
            else if (this.TryAdvance(TokenKind.StarAssignment)) {
                op = BinaryOperationKind.Multiply;
            }
            else if (this.TryAdvance(TokenKind.DivideAssignment)) {
                op = BinaryOperationKind.FloorDivide;
            }
            else if (this.TryAdvance(TokenKind.ModuloAssignment)) {
                op = BinaryOperationKind.Modulo;
            }
            else {
                return start;
            }

            var second = this.TopExpression();
            var loc = start.Location.Span(second.Location);

            var assign = new BinaryParseSyntax {
                Location = loc,
                Left = start,
                Right = second,
                Operator = op
            };

            var stat = new AssignmentParseStatement {
                Location = loc,
                Left = start,
                Right = assign
            };
            
            return stat;
        }
    }
}