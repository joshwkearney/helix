namespace Trophy.Parsing {
    public partial class Parser {
        private int pos = 0;
        private readonly IReadOnlyList<Token> tokens;

        public Parser(IReadOnlyList<Token> tokens) {
            this.tokens = tokens;
        }

        public IReadOnlyList<IDeclarationTree> Parse() {
            var list = new List<IDeclarationTree>();

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

        /** Declaration Parsing **/
        private IDeclarationTree Declaration() {
            if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionDeclaration();
            }
            else if (this.Peek(TokenKind.StructKeyword) || this.Peek(TokenKind.UnionKeyword)) {
                return this.AggregateDeclaration();
            }

            throw ParsingErrors.UnexpectedToken(this.Advance());
        }

        /** Expression Parsing **/
        private ISyntaxTree TopExpression() => this.AsExpression();

        private ISyntaxTree BinaryExpression() => this.OrExpression();

        private ISyntaxTree PrefixExpression() => this.UnaryExpression();        

        private ISyntaxTree SuffixExpression() {
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

        private ISyntaxTree Atom() {
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
            else if (this.Peek(TokenKind.IntKeyword)) {
                var tok = this.Advance(TokenKind.IntKeyword);

                return new IdenfifierAccessParseTree(tok.Location, "int");
            }
            else if (this.Peek(TokenKind.BoolKeyword)) {
                var tok = this.Advance(TokenKind.BoolKeyword);

                return new IdenfifierAccessParseTree(tok.Location, "bool");
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
            else {
                result = this.AssignmentStatement();
            }

            this.Advance(TokenKind.Semicolon);

            return result;
        }    
    }
}