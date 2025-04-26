using Helix.Syntax;
using Helix.Features.Variables.ParseSyntax;

namespace Helix.Parsing {
    public partial class Parser {
        private readonly Lexer lexer;
        private readonly Stack<bool> isInLoop = new();

        public Parser(string text) {
            this.lexer = new Lexer(text);
            this.isInLoop.Push(false);
        }

        public IReadOnlyList<IDeclaration> Parse() {
            var list = new List<IDeclaration>();

            while (this.lexer.PeekToken().Kind != TokenKind.EOF) {
                list.Add(this.Declaration());
            }

            return list;
        }

        private bool Peek(TokenKind kind) {
            return this.lexer.PeekToken().Kind == kind;
        }

        private bool TryAdvance(TokenKind kind) {
            if (this.Peek(kind)) {
                this.Advance(kind);
                return true;
            }

            return false;
        }

        private Token Advance() {
            var tok = this.lexer.GetToken();

            if (tok.Kind == TokenKind.EOF) {
                throw ParseException.EndOfFile(new TokenLocation());
            }

            return tok;
        }

        private Token Advance(TokenKind kind) {
            var tok = this.Advance();

            if (tok.Kind != kind) {
                throw ParseException.UnexpectedToken(kind, tok);
            }

            return tok;
        }        

        /** Declaration Parsing **/
        private IDeclaration Declaration() {
            if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionDeclaration();
            }
            else if (this.Peek(TokenKind.StructKeyword)) {
                return this.StructDeclaration();
            }
            else if (this.Peek(TokenKind.UnionKeyword)) {
                return this.UnionDeclaration();
            }
            else if (this.Peek(TokenKind.ExternKeyword)) {
                return this.ExternFunctionDeclaration();
            }

            throw ParseException.UnexpectedToken(this.Advance());
        }

        /** Expression Parsing **/
        private IParseSyntax TopExpression() => this.BinaryExpression();

        private IParseSyntax BinaryExpression() => this.OrExpression();

        private IParseSyntax PrefixExpression() => this.AsExpression();     

        private IParseSyntax SuffixExpression() {
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

        private IParseSyntax Atom() {
            if (this.Peek(TokenKind.Identifier)) {
                return this.VariableAccess();
            }
            else if (this.Peek(TokenKind.WordLiteral)) {
                return this.WordLiteral();
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
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.Block();
            }
            else if (this.Peek(TokenKind.WordKeyword)) {
                var tok = this.Advance(TokenKind.WordKeyword);

                return new VariableAccessParseSyntax {
                    Location = tok.Location,
                    VariableName = "word"
                };
            }
            else if (this.Peek(TokenKind.BoolKeyword)) {
                var tok = this.Advance(TokenKind.BoolKeyword);

                return new VariableAccessParseSyntax {
                    Location = tok.Location,
                    VariableName = "bool"
                };
            }
            else if (this.Peek(TokenKind.NewKeyword)) {
                return this.NewExpression();
            }
            else if (this.Peek(TokenKind.OpenBracket)) {
                return this.ArrayLiteral();
            }
            else {
                var next = this.Advance();

                throw ParseException.UnexpectedToken(next);
            }
        }        

        private IParseSyntax ParenExpression() {
            this.Advance(TokenKind.OpenParenthesis);
            var result = this.TopExpression();
            this.Advance(TokenKind.CloseParenthesis);

            return result;
        }

        private IParseSyntax Statement() {
            IParseSyntax result;

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