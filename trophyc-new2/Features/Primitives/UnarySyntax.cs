using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Trophy.Analysis;
using Trophy.Analysis.SyntaxTree;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing {
    public partial class Parser {
        private IParseTree UnaryExpression() {
            if (this.Peek(TokenKind.Subtract) || this.Peek(TokenKind.Add) || this.Peek(TokenKind.Not)) {
                var tokOp = this.Advance();
                var first = this.SuffixExpression();
                var loc = tokOp.Location.Span(first.Location);
                var op = UnaryOperator.Not;

                if (tokOp.Kind == TokenKind.Add) {
                    op = UnaryOperator.Plus;
                }
                else if (tokOp.Kind == TokenKind.Subtract) {
                    op = UnaryOperator.Minus;
                }

                return new UnaryParseSyntax(loc, op, first);
            }

            return this.SuffixExpression();
        }
    }
}

namespace Trophy.Features.Primitives {
    public enum UnaryOperator {
        Not, Plus, Minus
    }

    public class UnaryParseSyntax : IParseTree {
        private readonly UnaryOperator op;
        private readonly IParseTree arg;

        public TokenLocation Location { get; }

        public UnaryParseSyntax(TokenLocation location, UnaryOperator op, IParseTree arg) {
            this.Location = location;
            this.op = op;
            this.arg = arg;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            var syntax = this.arg.ResolveTypes(scope, names, types);

            if (this.op == UnaryOperator.Plus || this.op == UnaryOperator.Minus) {
                if (syntax.TryUnifyTo(PrimitiveType.Int).TryGetValue(out var newSyntax)) {
                    syntax = newSyntax;
                }
                else { 
                    throw TypeCheckingErrors.UnexpectedType(this.Location, PrimitiveType.Int, syntax.ReturnType);
                }

                if (op == UnaryOperator.Minus) {
                    syntax = new BinarySyntax(
                        new IntLiteral(0), 
                        syntax, 
                        BinaryOperation.Subtract, 
                        PrimitiveType.Int);
                }

                return syntax;
            }
            else if (this.op == UnaryOperator.Not) {
                if (syntax.TryUnifyTo(PrimitiveType.Bool).TryGetValue(out var newSyntax)) {
                    syntax = newSyntax;
                }
                else {
                    throw TypeCheckingErrors.UnexpectedType(this.Location, PrimitiveType.Int, syntax.ReturnType);
                }

                syntax = new BinarySyntax(
                        new BoolLiteral(true),
                        syntax,
                        BinaryOperation.Xor,
                        PrimitiveType.Bool);

                return syntax;
            }
            else {
                throw new Exception("Unexpected unary operator type");
            }
        }
    }
}