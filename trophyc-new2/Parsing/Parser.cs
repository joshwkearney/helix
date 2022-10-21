using Trophy.Analysis;

namespace Trophy.Parsing {
    public class BlockBuilder {
        private static int counter = 0;

        public List<ISyntaxTree> Statements { get; } = new();

        public string GetTempName() => "$" + counter++;
    }

    public partial class Parser {
        private IdentifierPath scope = new();
        private readonly Lexer lexer;

        public Parser(string text) {
            this.lexer = new Lexer(text);
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
        private ISyntaxTree TopExpression(BlockBuilder block) => this.AsExpression(block);

        private ISyntaxTree BinaryExpression(BlockBuilder block) => this.OrExpression(block);

        private ISyntaxTree PrefixExpression(BlockBuilder block) => this.UnaryExpression(block);        

        private ISyntaxTree SuffixExpression(BlockBuilder block) {
            var first = this.Atom(block);

            while (this.Peek(TokenKind.OpenParenthesis) 
                || this.Peek(TokenKind.Dot) 
                || this.Peek(TokenKind.Multiply)
                || this.Peek(TokenKind.OpenBracket)) {
                //|| this.Peek(TokenKind.Caret)) {

                if (this.Peek(TokenKind.OpenParenthesis)) {
                    first = this.InvokeExpression(first, block);
                }
                else if (this.Peek(TokenKind.Dot)) {
                    first = this.MemberAccess(first, block);
                }
                else if (this.Peek(TokenKind.Multiply) || this.Peek(TokenKind.Caret)) {
                    first = this.TypePointer(first, block);
                }
                else if (this.Peek(TokenKind.OpenBracket)) {
                    first = this.ArrayExpression(first, block);
                }
                else {
                    throw new Exception("Unexpected suffix token");
                }
            }

            return first;
        }        

        private ISyntaxTree Atom(BlockBuilder block) {
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
                return this.ParenExpression(block);
            }
            else if (this.Peek(TokenKind.BoolLiteral)) {
                return this.BoolLiteral();
            }
            else if (this.Peek(TokenKind.IfKeyword)) {
                return this.IfExpression(block);
            }     
            else if (this.Peek(TokenKind.VarKeyword) || this.Peek(TokenKind.LetKeyword)) {
                return this.VarExpression(block);
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.Block(block);
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
                return this.PutExpression(block);
            }
            else {
                var next = this.Advance();

                throw ParsingErrors.UnexpectedToken(next);
            }
        }        

        private ISyntaxTree ParenExpression(BlockBuilder block) {
            this.Advance(TokenKind.OpenParenthesis);
            var result = this.TopExpression(block);
            this.Advance(TokenKind.CloseParenthesis);

            return result;
        }

        private ISyntaxTree Statement(BlockBuilder block) {
            ISyntaxTree result;

            if (this.Peek(TokenKind.WhileKeyword)) {
                result = this.WhileStatement(block);
            }
            else if (this.Peek(TokenKind.ForKeyword)) {
                result = this.ForStatement(block);
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                result = this.Block(block);
            }
            else {
                result = this.AssignmentStatement(block);
            }

            this.Advance(TokenKind.Semicolon);

            return result;
        }    
    }
}