using System.Collections.Immutable;
using Helix.Features.Functions;
using Helix.Features.Functions.ParseSyntax;
using Helix.Features.Primitives.Syntax;
using Helix.Syntax;

namespace Helix.Parsing;

public partial class Parser {
    private ParseFunctionSignature FunctionSignature() {
        var start = this.Advance(TokenKind.FunctionKeyword);
        var funcName = this.Advance(TokenKind.Identifier).Value;

        this.Advance(TokenKind.OpenParenthesis);

        var pars = ImmutableList<ParseFunctionParameter>.Empty;
        while (!this.Peek(TokenKind.CloseParenthesis)) {
            var parStart = this.Advance(TokenKind.VarKeyword);
            var parName = this.Advance(TokenKind.Identifier).Value;
            this.Advance(TokenKind.AsKeyword);

            var parType = this.TopExpression();
            var parLoc = parStart.Location.Span(parType.Location);

            if (!this.Peek(TokenKind.CloseParenthesis)) {
                this.Advance(TokenKind.Comma);
            }

            pars = pars.Add(new ParseFunctionParameter {
                Location = parLoc,
                Name = parName,
                Type = parType
            });
        }

        var end = this.Advance(TokenKind.CloseParenthesis);
        var returnType = new VoidLiteral { Location = end.Location} as IParseSyntax;

        if (this.TryAdvance(TokenKind.AsKeyword)) {
            returnType = this.TopExpression();
        }

        var loc = start.Location.Span(returnType.Location);

        var sig = new ParseFunctionSignature {
            Location = loc,
            Name = funcName,
            ReturnType = returnType,
            Parameters = pars
        };

        return sig;
    }

    private IDeclaration FunctionDeclaration() {
        var sig = this.FunctionSignature();

        if (!this.Peek(TokenKind.OpenBrace)) {
            this.Advance(TokenKind.Yields);
        }

        var body = this.TopExpression();           
        this.Advance(TokenKind.Semicolon);

        return new FunctionParseDeclaration(
            sig.Location.Span(body.Location), 
            sig,
            body);
    }
    
    private IDeclaration ExternFunctionDeclaration() {
        var start = this.Advance(TokenKind.ExternKeyword);
        var sig = this.FunctionSignature();
        var end = this.Advance(TokenKind.Semicolon);
        var loc = start.Location.Span(end.Location);

        return new ExternFunctionParseDeclaration {
            Location = loc,
            Signature = sig
        };
    }
    
    private IParseSyntax InvokeExpression(IParseSyntax first) {
        this.Advance(TokenKind.OpenParenthesis);

        var args = new List<IParseSyntax>();

        while (!this.Peek(TokenKind.CloseParenthesis)) {
            args.Add(this.TopExpression());

            if (!this.TryAdvance(TokenKind.Comma)) {
                break;
            }
        }

        var last = this.Advance(TokenKind.CloseParenthesis);
        var loc = first.Location.Span(last.Location);

        return new InvokeParseSyntax {
            Location = loc,
            Operand = first,
            Arguments = args
        };
    }
    
    private IParseSyntax ReturnStatement() {
        var start = this.Advance(TokenKind.ReturnKeyword);
        var arg = this.TopExpression();

        return new ReturnParseSyntax {
            Location = start.Location.Span(arg.Location),
            Operand = arg
        };
    }
}