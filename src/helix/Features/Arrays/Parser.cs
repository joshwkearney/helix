using Helix.Features.Arrays;
using Helix.Syntax;

namespace Helix.Parsing;

public partial class Parser {
    public IParseSyntax ArrayExpression(IParseSyntax start) {
        this.Advance(TokenKind.OpenBracket);

        if (this.Peek(TokenKind.CloseBracket)) {
            var end = this.Advance(TokenKind.CloseBracket);
            var loc = start.Location.Span(end.Location);

            return new ArrayTypeParseSyntax {
                Location = loc,
                Operand = start
            };
        }
        else {
            var index = this.TopExpression();
            var end = this.Advance(TokenKind.CloseBracket);
            var loc = start.Location.Span(end.Location);

            return new ArrayIndexParseSyntax {
                Location = loc,
                Operand = start,
                Index = index
            };
        }            
    }
    
    private IParseSyntax ArrayLiteral() {
        var start = this.Advance(TokenKind.OpenBracket);
        var args = new List<IParseSyntax>();

        while (!this.Peek(TokenKind.CloseBracket)) {
            args.Add(this.TopExpression());

            if (!this.Peek(TokenKind.CloseBracket)) {
                this.Advance(TokenKind.Comma);
            }
        }

        var end = this.Advance(TokenKind.CloseBracket);
        var loc = start.Location.Span(end.Location);

        return new ArrayLiteralParseSyntax {
            Location = loc,
            Arguments = args
        };
    }
}