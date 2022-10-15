using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Features.Variables;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing {
    public partial class Parser {
        private int pos = 0;
        private readonly IReadOnlyList<Token> tokens;

        public Parser(IReadOnlyList<Token> tokens) {
            this.tokens = tokens;
        }

        public IReadOnlyList<IParseDeclaration> Parse() {
            var list = new List<IParseDeclaration>();

            while (pos < tokens.Count) {
                list.Add(this.Declaration());
            }

            return list;
        }

        private bool Peek(TokenKind kind) {
            return this.Peek(kind, 1);
        }

        private bool Peek(TokenKind kind, int count) {
            if (this.pos + count - 1 >= this.tokens.Count) {
                return false;
            }

            return this.tokens[this.pos + count - 1].Kind == kind;
        }

        private bool TryAdvance(TokenKind kind) {
            if (this.Peek(kind)) {
                this.Advance(kind);
                return true;
            }

            return false;
        }

        private Token Advance() {
            if (this.pos >= this.tokens.Count) {
                throw ParsingErrors.EndOfFile(this.tokens.Last().Location);
            }

            return this.tokens[this.pos++];
        }

        private Token Advance(TokenKind kind) {
            var tok = this.Advance();

            if (tok.Kind != kind) {
                throw ParsingErrors.UnexpectedToken(kind, tok);
            }

            return tok;
        }        

        /** Type Parsing **/
        private ITypeTree TypeExpression() {
            return this.TypeAtom();
        }        

        private ITypeTree TypeAtom() {
            if (this.Peek(TokenKind.IntKeyword)) {
                var tok = this.Advance(TokenKind.IntKeyword);

                return new PrimitiveTypeTree(tok.Location, PrimitiveType.Int);
            }
            else if (this.Peek(TokenKind.VoidKeyword)) {
                var tok = this.Advance(TokenKind.VoidKeyword);

                return new PrimitiveTypeTree(tok.Location, PrimitiveType.Void);
            }
            else if (this.Peek(TokenKind.BoolKeyword)) {
                var tok = this.Advance(TokenKind.BoolKeyword);

                return new PrimitiveTypeTree(tok.Location, PrimitiveType.Bool);
            }
            else {
                Token tok = this.Advance(TokenKind.Identifier);

                return new TypeVariableAccess(tok.Location, tok.Value);
            }
        }

        /** Declaration Parsing **/
        private IParseDeclaration Declaration() {
            if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionDeclaration();
            }
            else if (this.Peek(TokenKind.StructKeyword) || this.Peek(TokenKind.UnionKeyword)) {
                return this.AggregateDeclaration();
            }

            throw ParsingErrors.UnexpectedToken(this.Advance());
        }

        /** Expression Parsing **/
        private IParseTree TopExpression() => this.AsExpression();

        private IParseTree BinaryExpression() => this.OrExpression();

        private IParseTree PrefixExpression() => this.UnaryExpression();        

        private IParseTree SuffixExpression() {
            var first = this.Atom();

            while (this.Peek(TokenKind.OpenParenthesis) || this.Peek(TokenKind.Dot)) {
                if (this.Peek(TokenKind.OpenParenthesis)) {
                    first = this.InvokeExpression(first);
                }
                else if (this.Peek(TokenKind.Dot)) {
                    first = this.MemberAccess(first);
                }
                else {
                    throw new Exception("Unexpected suffix token");
                }
            }

            return first;
        }        

        private IParseTree Atom() {
            if (this.Peek(TokenKind.Identifier)) {
                return this.VariableAccess();
            }
            else if (this.Peek(TokenKind.IntLiteral)) {
                return this.IntLiteral();
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.Block();
            }
            else if (this.Peek(TokenKind.VoidKeyword)) {
                return this.VoidLiteral();
            }
            else if (this.Peek(TokenKind.OpenParenthesis)) {
                return this.ParenExpression();
            }
            else if (this.Peek(TokenKind.BoolLiteral)) {
                return this.BoolLiteral();
            }
            else if (this.Peek(TokenKind.IfKeyword)) {
                return this.IfExpression();
            }     
            else if (this.Peek(TokenKind.VarKeyword)) {
                return this.VarExpression();
            }
            else {
                var next = this.Advance();

                throw ParsingErrors.UnexpectedToken(next);
            }
        }        

        private IParseTree ParenExpression() {
            this.Advance(TokenKind.OpenParenthesis);
            var result = this.TopExpression();
            this.Advance(TokenKind.CloseParenthesis);

            return result;
        }

        private IParseTree Statement() {
            IParseTree result;

            if (this.Peek(TokenKind.WhileKeyword)) {
                result = this.WhileStatement();
            }
            else if (this.Peek(TokenKind.ForKeyword)) {
                result = this.ForStatement();
            }
            //else if (this.Peek(TokenKind.ReturnKeyword)) {
            //    result = this.ReturnStatement();
            //}
            else {
                result = this.AssignmentStatement();
            }

            this.Advance(TokenKind.Semicolon);

            return result;
        }

        private IParseTree AssignmentStatement() {
            var start = this.TopExpression();

            if (this.TryAdvance(TokenKind.Assignment)) {
                var assign = this.TopExpression();
                var loc = start.Location.Span(assign.Location);

                return new AssignmentParseTree(loc, start, assign);
            }

            return start;
        }        
    }
}