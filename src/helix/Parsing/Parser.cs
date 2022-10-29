using Helix.Analysis;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        private IdentifierPath scope = new();
        private readonly Lexer lexer;
        private readonly Stack<bool> isInLoop = new();
        private readonly Stack<IdentifierPath> funcPath = new();

        public Parser(string text) {
            this.lexer = new Lexer(text);
            this.isInLoop.Push(false);
        }

        public IReadOnlyList<IDeclaration> Parse() {
            var list = new List<IDeclaration>();

            while (this.lexer.PeekToken(this.scope).Kind != TokenKind.EOF) {
                list.Add(this.Declaration());
            }

            return list;
        }

        private bool Peek(TokenKind kind) {
            return this.lexer.PeekToken(this.scope).Kind == kind;
        }

        private bool TryAdvance(TokenKind kind) {
            if (this.Peek(kind)) {
                this.Advance(kind);
                return true;
            }

            return false;
        }

        private Token Advance() {
            var tok = this.lexer.GetToken(this.scope);

            if (tok.Kind == TokenKind.EOF) {
                throw ParsingErrors.EndOfFile(new TokenLocation());
            }

            return tok;
        }

        private Token Advance(TokenKind kind) {
            var tok = this.Advance();

            if (tok.Kind != kind) {
                throw ParsingErrors.UnexpectedToken(kind, tok);
            }

            return tok;
        }        

        /** Declaration Parsing **/
        private IDeclaration Declaration() {
            if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionDeclaration();
            }
            else if (this.Peek(TokenKind.StructKeyword) || this.Peek(TokenKind.UnionKeyword)) {
                return this.AggregateDeclaration();
            }
            else if (this.Peek(TokenKind.ExternKeyword)) {
                return this.ExternFunctionDeclaration();
            }

            throw ParsingErrors.UnexpectedToken(this.Advance());
        }

        /** Expression Parsing **/
        private ISyntaxTree TopExpression() => this.AsExpression();

        private ISyntaxTree BinaryExpression() => this.OrExpression();

        private ISyntaxTree PrefixExpression() => this.UnaryExpression();        

        private ISyntaxTree SuffixExpression() {
            var first = this.Atom();

            while (this.Peek(TokenKind.OpenParenthesis) 
                || this.Peek(TokenKind.Dot) 
                || this.Peek(TokenKind.OpenBracket)
                || this.Peek(TokenKind.Star)) {

                if (this.Peek(TokenKind.OpenParenthesis)) {
                    first = this.InvokeExpression(first);
                }
                else if (this.Peek(TokenKind.Dot)) {
                    first = this.MemberAccess(first);
                }
                else if (this.Peek(TokenKind.OpenBracket)) {
                    first = this.ArrayExpression(first);
                }
                else if (this.Peek(TokenKind.Star)) {
                    first = this.DereferenceExpression(first);
                }
                else {
                    throw new Exception("Unexpected suffix token");
                }
            }

            return first;
        }        

        private ISyntaxTree Atom() {
            if (this.Peek(TokenKind.Identifier)) {
                return this.VariableAccess();
            }
            else if (this.Peek(TokenKind.IntLiteral)) {
                return this.IntLiteral();
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
            else if (this.Peek(TokenKind.VarKeyword) || this.Peek(TokenKind.LetKeyword)) {
                return this.VarExpression();
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.Block();
            }
            else if (this.Peek(TokenKind.IntKeyword)) {
                var tok = this.Advance(TokenKind.IntKeyword);

                return new VariableAccessParseSyntax(tok.Location, "int");
            }
            else if (this.Peek(TokenKind.BoolKeyword)) {
                var tok = this.Advance(TokenKind.BoolKeyword);

                return new VariableAccessParseSyntax(tok.Location, "bool");
            }
            else if (this.Peek(TokenKind.PutKeyword)) {
                return this.PutExpression();
            }
            else if (this.Peek(TokenKind.NewKeyword)) {
                return this.NewExpression();
            }
            else {
                var next = this.Advance();

                throw ParsingErrors.UnexpectedToken(next);
            }
        }        

        private ISyntaxTree ParenExpression() {
            this.Advance(TokenKind.OpenParenthesis);
            var result = this.TopExpression();
            this.Advance(TokenKind.CloseParenthesis);

            return result;
        }

        private ISyntaxTree Statement() {
            ISyntaxTree result;

            if (this.Peek(TokenKind.WhileKeyword)) {
                result = this.WhileStatement();
            }
            else if (this.Peek(TokenKind.ForKeyword)) {
                result = this.ForStatement();
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                result = this.Block();
            }
            else if (this.Peek(TokenKind.BreakKeyword) || this.Peek(TokenKind.ContinueKeyword)) {
                result = this.BreakStatement();
            }
            else if (this.Peek(TokenKind.ReturnKeyword)) {
                result = this.ReturnStatement();
            }
            else {
                result = this.AssignmentStatement();
            }

            this.Advance(TokenKind.Semicolon);

            return result;
        }    
    }
}