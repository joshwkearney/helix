using helix.common;
using Helix.Analysis.Types;
using Helix.Parsing;
using System.Collections.Immutable;

namespace Helix.Frontend.ParseTree {
    internal class Parser {
        private readonly Lexer lexer;
        private readonly Stack<bool> isInLoop = new();

        public Parser(string text) {
            lexer = new Lexer(text);
            isInLoop.Push(false);
        }

        public IReadOnlyList<IParseTree> Parse() {
            var list = new List<IParseTree>();

            while (lexer.PeekToken().Kind != TokenKind.EOF) {
                list.Add(Declaration());
            }

            return list;
        }

        private bool Peek(TokenKind kind) {
            return lexer.PeekToken().Kind == kind;
        }

        private bool TryAdvance(TokenKind kind) {
            if (Peek(kind)) {
                Advance(kind);
                return true;
            }

            return false;
        }

        private Token Advance() {
            var tok = lexer.GetToken();

            if (tok.Kind == TokenKind.EOF) {
                throw ParseException.EndOfFile(new TokenLocation(0, 0, 0));
            }

            return tok;
        }

        private Token Advance(TokenKind kind) {
            var tok = Advance();

            if (tok.Kind != kind) {
                throw ParseException.UnexpectedToken(kind, tok);
            }

            return tok;
        }

        /** Type Parsing **/
        private IHelixType ParseType() {
            return this.TypeAtom();
        }

        private IHelixType TypeAtom() {
            if (this.Peek(TokenKind.WordLiteral)) {
                var value = long.Parse(this.Advance(TokenKind.WordLiteral).Value);

                return new SingularWordType() { Value = value };
            }
            else if (this.TryAdvance(TokenKind.TrueKeyword)) {
                return new SingularBoolType() { Value = true };
            }
            else if (this.TryAdvance(TokenKind.FalseKeyword)) {
                return new SingularBoolType() { Value = false };
            }
            else if (this.TryAdvance(TokenKind.VoidKeyword)) {
                return new VoidType();
            }
            else if (this.TryAdvance(TokenKind.WordKeyword)) {
                return new WordType();
            }
            else if (this.TryAdvance(TokenKind.BoolKeyword)) {
                return new BoolType();
            }
            else if (this.Peek(TokenKind.Identifier)) {
                var name = this.Advance(TokenKind.Identifier).Value;

                return new NominalType() { Name = name, DisplayName = name };
            }
            else {
                throw ParseException.UnexpectedToken(this.Advance());
            }
        }

        /** Declaration Parsing **/
        private IParseTree Declaration() {
            if (Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionDeclaration();
            }
            else if (Peek(TokenKind.StructKeyword)) {
                return this.StructDeclaration();
            }
            else if (Peek(TokenKind.UnionKeyword)) {
                return this.UnionDeclaration();
            }

            throw ParseException.UnexpectedToken(Advance());
        }

        private (FunctionType sig, string name, TokenLocation loc) FunctionSignature() {
            var start = this.Advance(TokenKind.FunctionKeyword);
            var funcName = this.Advance(TokenKind.Identifier).Value;

            this.Advance(TokenKind.OpenParenthesis);

            var pars = ImmutableList<FunctionParameter>.Empty;
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var parStart = this.Advance(TokenKind.VarKeyword);
                var parName = this.Advance(TokenKind.Identifier).Value;
                this.Advance(TokenKind.AsKeyword);

                var parType = this.ParseType();

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars = pars.Add(new FunctionParameter() { IsMutable = true, Name = parName, Type = parType });
            }

            var end = this.Advance(TokenKind.CloseParenthesis);
            IHelixType returnType = new VoidType();

            if (this.TryAdvance(TokenKind.AsKeyword)) {
                returnType = this.ParseType();
            }

            var func = new FunctionType() { ReturnType = returnType, Parameters = pars };
            var loc = start.Location.Span(end.Location);

            return (func, funcName, loc);
        }

        private IParseTree FunctionDeclaration() {
            var (sig, name, loc) = this.FunctionSignature();

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            var body = this.TopExpression();
            this.Advance(TokenKind.Semicolon);

            return new FunctionDeclaration() {
                Location = loc.Span(body.Location),
                Name = name,
                Signature = sig,
                Body = body
            };
        }

        private IParseTree StructDeclaration() {
            var start = this.Advance(TokenKind.StructKeyword);
            var name = this.Advance(TokenKind.Identifier).Value;
            var mems = new List<StructMember>();

            this.Advance(TokenKind.OpenBrace);

            while (!this.Peek(TokenKind.CloseBrace)) {
                this.Advance(TokenKind.VarKeyword);

                var memName = this.Advance(TokenKind.Identifier);
                this.Advance(TokenKind.AsKeyword);

                var memType = this.ParseType();
                this.Advance(TokenKind.Semicolon);

                mems.Add(new StructMember() { 
                    IsMutable = true, 
                    Name = memName.Value, 
                    Type = memType 
                });
            }

            this.Advance(TokenKind.CloseBrace);

            var last = this.Advance(TokenKind.Semicolon);
            var loc = start.Location.Span(last.Location);

            return new StructDeclaration() {
                Location = loc,
                Name = name,
                Signature = new StructType() {
                    Members = mems
                }
            };
        }

        private IParseTree UnionDeclaration() {
            var start = this.Advance(TokenKind.UnionKeyword);
            var name = this.Advance(TokenKind.Identifier).Value;
            var mems = new List<UnionMember>();

            this.Advance(TokenKind.OpenBrace);

            while (!this.Peek(TokenKind.CloseBrace)) {
                this.Advance(TokenKind.VarKeyword);

                var memName = this.Advance(TokenKind.Identifier);
                this.Advance(TokenKind.AsKeyword);

                var memType = this.ParseType();
                this.Advance(TokenKind.Semicolon);

                mems.Add(new UnionMember() { 
                    IsMutable = true, 
                    Name = memName.Value, 
                    Type = memType 
                });
            }

            this.Advance(TokenKind.CloseBrace);

            var last = this.Advance(TokenKind.Semicolon);
            var loc = start.Location.Span(last.Location);

            return new UnionDeclaration() {
                Location = loc,
                Name = name,
                Signature = new UnionType() {
                    Members = mems
                }
            };
        }

        /** Expression Parsing **/
        private IParseTree TopExpression() => BinaryExpression();

        private IParseTree BinaryExpression() => this.OrExpression();

        private IParseTree OrExpression() {
            var first = this.XorExpression();

            while (this.TryAdvance(TokenKind.OrKeyword)) {
                var branching = this.TryAdvance(TokenKind.ElseKeyword);
                var second = this.XorExpression();
                var loc = first.Location.Span(second.Location);

                if (branching) {
                    first = new BinarySyntax() {
                        Location = loc,
                        Left = first,
                        Right = second,
                        Operator = BinaryOperationKind.BranchingOr
                    };
                }
                else {
                    first = new BinarySyntax() {
                        Location = loc,
                        Left = first,
                        Right = second,
                        Operator = BinaryOperationKind.Or
                    };
                }
            }

            return first;
        }

        private IParseTree XorExpression() {
            var first = this.AndExpression();

            while (this.TryAdvance(TokenKind.XorKeyword)) {
                var second = this.AndExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax() {
                    Location = loc,
                    Left = first,
                    Right = second,
                    Operator = BinaryOperationKind.Xor
                };
            }

            return first;
        }

        private IParseTree AndExpression() {
            var first = this.ComparisonExpression();

            while (this.TryAdvance(TokenKind.AndKeyword)) {
                var branching = this.TryAdvance(TokenKind.ThenKeyword);
                var second = this.XorExpression();
                var loc = first.Location.Span(second.Location);

                if (branching) {
                    first = new BinarySyntax() {
                        Location = loc,
                        Left = first,
                        Right = second,
                        Operator = BinaryOperationKind.BranchingAnd
                    };
                }
                else {
                    first = new BinarySyntax() {
                        Location = loc,
                        Left = first,
                        Right = second,
                        Operator = BinaryOperationKind.And
                    };
                }
            }

            return first;
        }

        private IParseTree ComparisonExpression() {
            var first = this.AddExpression();
            var comparators = new Dictionary<TokenKind, BinaryOperationKind>() {
                { TokenKind.Equals, BinaryOperationKind.EqualTo }, { TokenKind.NotEquals, BinaryOperationKind.NotEqualTo },
                { TokenKind.LessThan, BinaryOperationKind.LessThan }, { TokenKind.GreaterThan, BinaryOperationKind.GreaterThan },
                { TokenKind.LessThanOrEqualTo, BinaryOperationKind.LessThanOrEqualTo },
                { TokenKind.GreaterThanOrEqualTo, BinaryOperationKind.GreaterThanOrEqualTo }
            };

            while (true) {
                bool worked = false;

                foreach (var (tok, _) in comparators) {
                    worked |= this.Peek(tok);
                }

                if (!worked) {
                    break;
                }

                var op = comparators[this.Advance().Kind];
                var second = this.AddExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax() {
                    Location = loc,
                    Left = first,
                    Right = second,
                    Operator = op
                };
            }

            return first;
        }

        private IParseTree AddExpression() {
            var first = this.MultiplyExpression();

            while (true) {
                if (!this.Peek(TokenKind.Plus) && !this.Peek(TokenKind.Minus)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.Plus ? BinaryOperationKind.Add : BinaryOperationKind.Subtract;
                var second = this.MultiplyExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax() {
                    Location = loc,
                    Left = first,
                    Right = second,
                    Operator = op
                };
            }

            return first;
        }

        private IParseTree MultiplyExpression() {
            var first = this.PrefixExpression();

            while (true) {
                if (!this.Peek(TokenKind.Star) && !this.Peek(TokenKind.Modulo) && !this.Peek(TokenKind.Divide)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = BinaryOperationKind.Modulo;

                if (tok == TokenKind.Star) {
                    op = BinaryOperationKind.Multiply;
                }
                else if (tok == TokenKind.Divide) {
                    op = BinaryOperationKind.FloorDivide;
                }

                var second = this.PrefixExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax() {
                    Location = loc,
                    Left = first,
                    Right = second,
                    Operator = op
                };
            }

            return first;
        }

        private IParseTree PrefixExpression() => this.AsExpression();

        private IParseTree AsExpression() {
            var first = this.UnaryExpression();

            while (this.Peek(TokenKind.AsKeyword) || this.Peek(TokenKind.IsKeyword)) {
                if (this.TryAdvance(TokenKind.AsKeyword)) {
                    var type = this.ParseType();

                    first = new AsSyntax() {
                        Location = first.Location,
                        Operand = first,
                        Type = type
                    };
                }
                else {
                    this.Advance(TokenKind.IsKeyword);
                    var nameTok = this.Advance(TokenKind.Identifier);

                    first = new IsSyntax() {
                        Location = first.Location.Span(nameTok.Location),
                        Operand = first,
                        Field = nameTok.Value
                    };
                }
            }

            return first;
        }

        private IParseTree UnaryExpression() {
            var hasOperator = this.Peek(TokenKind.Minus)
                || this.Peek(TokenKind.Plus)
                || this.Peek(TokenKind.Not)
                || this.Peek(TokenKind.Ampersand);

            if (hasOperator) {
                var tokOp = this.Advance();
                var first = this.UnaryExpression();
                var loc = tokOp.Location.Span(first.Location);
                var op = UnaryOperatorKind.Not;

                if (tokOp.Kind == TokenKind.Plus) {
                    op = UnaryOperatorKind.Plus;
                }
                else if (tokOp.Kind == TokenKind.Minus) {
                    op = UnaryOperatorKind.Minus;
                }
                else if (tokOp.Kind == TokenKind.Ampersand) {
                    return new UnarySyntax() {
                        Location = loc,
                        Operand = first,
                        Operator = UnaryOperatorKind.AddressOf
                    };
                }
                else if (tokOp.Kind != TokenKind.Not) {
                    throw new Exception("Unexpected unary operator");
                }

                return new UnarySyntax() {
                    Location = loc,
                    Operand = first,
                    Operator = op
                };
            }

            return this.SuffixExpression();
        }

        private IParseTree SuffixExpression() {
            var first = Atom();

            while (Peek(TokenKind.OpenParenthesis)
                || Peek(TokenKind.Dot)
                || Peek(TokenKind.OpenBracket)
                || Peek(TokenKind.Star)) {

                if (Peek(TokenKind.OpenParenthesis)) {
                    first = this.InvokeExpression(first);
                }
                else if (Peek(TokenKind.Dot)) {
                    first = this.MemberAccess(first);
                }
                else if (Peek(TokenKind.OpenBracket)) {
                    first = this.IndexExpression(first);
                }
                else if (Peek(TokenKind.Star)) {
                    first = this.DereferenceExpression(first);
                }
                else {
                    throw new Exception("Unexpected suffix token");
                }
            }

            return first;
        }

        private IParseTree InvokeExpression(IParseTree first) {
            this.Advance(TokenKind.OpenParenthesis);

            var args = new List<IParseTree>();

            while (!this.Peek(TokenKind.CloseParenthesis)) {
                args.Add(this.TopExpression());

                if (!this.TryAdvance(TokenKind.Comma)) {
                    break;
                }
            }

            var last = this.Advance(TokenKind.CloseParenthesis);
            var loc = first.Location.Span(last.Location);

            return new InvokeSyntax() {
                Location = loc,
                Target = first,
                Args = args
            };
        }

        private IParseTree MemberAccess(IParseTree first) {
            this.Advance(TokenKind.Dot);

            var tok = this.Advance(TokenKind.Identifier);
            var loc = first.Location.Span(tok.Location);

            return new MemberAccessSyntax() {
                Location = loc,
                Target = first,
                Field = tok.Value
            };
        }

        public IParseTree IndexExpression(IParseTree start) {
            this.Advance(TokenKind.OpenBracket);

            var index = this.TopExpression();
            var end = this.Advance(TokenKind.CloseBracket);
            var loc = start.Location.Span(end.Location);

            return new BinarySyntax() {
                Location = loc,
                Left = start,
                Right = index,
                Operator = BinaryOperationKind.Index
            };
        }

        public IParseTree DereferenceExpression(IParseTree first) {
            var op = this.Advance(TokenKind.Star);
            var loc = first.Location.Span(op.Location);

            return new UnarySyntax() {
                Location = loc,
                Operand = first,
                Operator = UnaryOperatorKind.Dereference
            };
        }

        private IParseTree Atom() {
            if (Peek(TokenKind.Identifier)) {
                var tok = this.Advance(TokenKind.Identifier);

                return new VariableAccess() { Location = tok.Location, VariableName = tok.Value };
            }
            else if (Peek(TokenKind.WordLiteral)) {
                var tok = this.Advance(TokenKind.WordLiteral);
                var num = long.Parse(tok.Value);

                return new WordLiteral { Location = tok.Location, Value = num };
            }
            else if (Peek(TokenKind.VoidKeyword)) {
                var tok = this.Advance(TokenKind.VoidKeyword);

                return new VoidLiteral() { Location = tok.Location };
            }
            else if (Peek(TokenKind.OpenParenthesis)) {
                return ParenExpression();
            }
            else if (Peek(TokenKind.BoolLiteral)) {
                var start = this.Advance(TokenKind.BoolLiteral);
                var value = bool.Parse(start.Value);

                return new BoolLiteral() { Location = start.Location, Value = value };
            }
            else if (Peek(TokenKind.IfKeyword)) {
                return this.IfExpression();
            }
            else if (Peek(TokenKind.VarKeyword)) {
                return this.VarExpression();
            }
            else if (Peek(TokenKind.OpenBrace)) {
                return this.Block();
            }
            else if (Peek(TokenKind.NewKeyword)) {
                return this.NewExpression();
            }
            else if (Peek(TokenKind.OpenBracket)) {
                return this.ArrayLiteral();
            }
            else {
                var next = Advance();

                throw ParseException.UnexpectedToken(next);
            }
        }

        private IParseTree ParenExpression() {
            Advance(TokenKind.OpenParenthesis);
            var result = TopExpression();
            Advance(TokenKind.CloseParenthesis);

            return result;
        }

        private IParseTree IfExpression() {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.ThenKeyword);
            var affirm = this.TopExpression();

            if (this.TryAdvance(TokenKind.ElseKeyword)) {
                var neg = this.TopExpression();
                var loc = start.Location.Span(neg.Location);

                return new IfSyntax() {
                    Location = loc,
                    Condition = cond,
                    Affirmative = affirm,
                    Negative = Option.Some(neg)
                };
            }
            else {
                var loc = start.Location.Span(affirm.Location);

                return new IfSyntax() {
                    Location = loc,
                    Condition = cond,
                    Affirmative = affirm
                };
            }
        }

        private IParseTree VarExpression() {
            var startLok = this.Advance(TokenKind.VarKeyword).Location;
            var name = this.Advance(TokenKind.Identifier).Value;

            var type = (Option<IHelixType>)Option.None;
            if (this.TryAdvance(TokenKind.AsKeyword)) {
                type = this.ParseType();
            }

            this.Advance(TokenKind.Assignment);

            var assign = this.TopExpression();
            var loc = startLok.Span(assign.Location);

            return new VariableStatement() {
                Location = loc,
                VariableName = name,
                Value = assign
            };
        }

        private IParseTree Block() {
            var start = this.Advance(TokenKind.OpenBrace);
            var stats = new List<IParseTree>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                stats.Add(this.Statement());
                this.Advance(TokenKind.Semicolon);
            }

            var end = this.Advance(TokenKind.CloseBrace);

            return new BlockSyntax() { 
                Location = start.Location.Span(end.Location),
                Statements = stats
            };
        }

        private IParseTree NewExpression() {
            var start = this.Advance(TokenKind.NewKeyword);
            var targetType = this.ParseType();

            if (!this.TryAdvance(TokenKind.OpenBrace)) {
                return new NewSyntax() {
                    Location = start.Location,
                    Type = targetType
                };
            }

            var names = new List<NewFieldAssignment>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                Option<string> name = Option.None;

                if (this.Peek(TokenKind.Identifier)) {
                    name = this.Advance(TokenKind.Identifier).Value;
                    this.Advance(TokenKind.Assignment);
                }

                var value = this.TopExpression();

                names.Add(new NewFieldAssignment() {
                    Name = name,
                    Value = value
                });

                if (!this.Peek(TokenKind.CloseBrace)) {
                    this.Advance(TokenKind.Comma);
                }
            }

            var end = this.Advance(TokenKind.CloseBrace);
            var loc = start.Location.Span(end.Location);

            return new NewSyntax() {
                Location = loc,
                Type = targetType,
                Assignments = names
            };
        }

        private IParseTree ArrayLiteral() {
            var start = this.Advance(TokenKind.OpenBracket);
            var args = new List<IParseTree>();

            while (!this.Peek(TokenKind.CloseBracket)) {
                args.Add(this.TopExpression());

                if (!this.Peek(TokenKind.CloseBracket)) {
                    this.Advance(TokenKind.Comma);
                }
            }

            var end = this.Advance(TokenKind.CloseBracket);
            var loc = start.Location.Span(end.Location);

            return new ArrayLiteral() {
                Location = start.Location,
                Args = args
            };
        }

        /** Statement Parsing **/
        private IParseTree Statement() {
            IParseTree result;

            if (Peek(TokenKind.WhileKeyword)) {
                result = this.WhileStatement();
            }
            else if (Peek(TokenKind.ForKeyword)) {
                result = this.ForStatement();
            }
            else if (Peek(TokenKind.OpenBrace)) {
                result = this.Block();
            }
            else if (Peek(TokenKind.BreakKeyword)) {
                result = new BreakSyntax() { Location = this.Advance().Location };
            }
            else if (Peek(TokenKind.ContinueKeyword)) {
                result = new ContinueSyntax() { Location = this.Advance().Location };
            }
            else if (Peek(TokenKind.ReturnKeyword)) {
                result = this.ReturnStatement();
            }
            else {
                result = this.AssignmentStatement();
            }

            return result;
        }

        private IParseTree AssignmentStatement() {
            var start = this.TopExpression();

            if (this.TryAdvance(TokenKind.Assignment)) {
                var assign = this.TopExpression();
                var loc = start.Location.Span(assign.Location);

                return new AssignmentStatement() {
                    Location = loc,
                    Target = start,
                    Assign = assign
                };
            }
            else {
                BinaryOperationKind op;

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

                var assign = new BinarySyntax() {
                    Location = loc,
                    Left = start,
                    Right = second,
                    Operator = op
                };

                return new AssignmentStatement() {
                    Location = loc,
                    Target = start,
                    Assign = assign
                };
            }
        }

        private IParseTree WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            this.isInLoop.Push(true);
            var body = this.TopExpression();
            this.isInLoop.Pop();

            return new WhileSyntax() {
                Location = start.Location.Span(body.Location),
                Condition = cond,
                Body = body
            };
        }

        private IParseTree ForStatement() {
            var startTok = this.Advance(TokenKind.ForKeyword);
            var id = this.Advance(TokenKind.Identifier);

            this.Advance(TokenKind.Assignment);
            var startIndex = this.TopExpression();

            var inclusive = true;
            if (!this.TryAdvance(TokenKind.ToKeyword)) {
                this.Advance(TokenKind.UntilKeyword);
                inclusive = false;
            }

            var endIndex = this.TopExpression();          
            var loc = startTok.Location.Span(endIndex.Location);

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            var body = this.TopExpression();
            loc = loc.Span(body.Location);

            return new ForSyntax() {
                Location = loc,
                Variable = id.Value,
                InitialValue = startIndex,
                FinalValue = endIndex,
                Inclusive = inclusive,
                Body = body
            };
        }

        public IParseTree ReturnStatement() {
            var start = this.Advance(TokenKind.ReturnKeyword);

            if (this.Peek(TokenKind.Semicolon)) {
                return new ReturnSyntax() { 
                    Location = start.Location, 
                    Payload = new VoidLiteral() { Location = start.Location } 
                };
            }
            else {
                return new ReturnSyntax() { 
                    Location = start.Location, 
                    Payload = this.TopExpression() 
                };
            }
        }
    }
}