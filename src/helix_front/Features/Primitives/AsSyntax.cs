using Helix.Analysis;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.Unions;
using Helix.Analysis.Types;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseTree AsExpression() {
            var first = this.UnaryExpression();

            while (this.Peek(TokenKind.AsKeyword) || this.Peek(TokenKind.IsKeyword)) {
                if (this.TryAdvance(TokenKind.AsKeyword)) {
                    var target = this.TopExpression();
                    var loc = first.Location.Span(target.Location);

                    first = new AsParseSyntax(loc, first, target);
                }
                else {
                    this.Advance(TokenKind.IsKeyword);
                    var nameTok = this.Advance(TokenKind.Identifier);

                    first = new IsParseSyntax() {
                        Location = first.Location.Span(nameTok.Location),
                        Target = first,
                        MemberName = nameTok.Value
                    };
                }
            }

            return first;
        }
    }
}

namespace Helix.Features.Primitives {
    public record AsParseSyntax(
        TokenLocation Location, 
        IParseTree Argument, 
        IParseTree TargetType) : IParseTree {

        public ImperativeExpression ToImperativeSyntax(ImperativeSyntaxWriter writer) => throw new NotImplementedException();
    }

    public record AsStatement(
        TokenLocation Location, 
        string VariableName, 
        ImperativeExpression Value, 
        HelixType TargetType) : IVariableStatement {

        public void CheckTypes(TypeFrame types, ImperativeSyntaxWriter writer) {
            var newValue = this.Value.UnifyTo(this.TargetType, types, writer);

            var stat = new VariableStatement() {
                Location = this.Location,
                Value = newValue,
                VariableName = this.VariableName
            };

            stat.CheckTypes(types, writer);
        }

        public string[] Write() {
            return new[] { $"let {this.VariableName} = {this.Value} as {this.TargetType};" };
        }
    }
}