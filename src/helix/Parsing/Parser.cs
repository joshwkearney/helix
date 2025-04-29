using System.Collections.Immutable;
using Helix.Syntax;
using Helix.Syntax.ParseTree;
using Helix.Syntax.ParseTree.Arrays;
using Helix.Syntax.ParseTree.FlowControl;
using Helix.Syntax.ParseTree.Functions;
using Helix.Syntax.ParseTree.Primitives;
using Helix.Syntax.ParseTree.Structs;
using Helix.Syntax.ParseTree.Unions;
using Helix.Syntax.ParseTree.Variables;
using Helix.Syntax.TypedTree.FlowControl;
using Helix.Syntax.TypedTree.Primitives;
using Helix.Types;
using ExpressionStatement = Helix.Syntax.ParseTree.FlowControl.ExpressionStatement;
using LoopStatement = Helix.Syntax.ParseTree.FlowControl.LoopStatement;

namespace Helix.Parsing;

public class Parser {
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
        
    private ParseFunctionSignature FunctionSignature() {
        var start = this.Advance(TokenKind.FunctionKeyword);
        var funcName = this.Advance(TokenKind.Identifier).Value;

        this.Advance(TokenKind.OpenParenthesis);

        var pars = ImmutableList<ParseFunctionParameter>.Empty;
        while (!this.Peek(TokenKind.CloseParenthesis)) {
            var parName = this.Advance(TokenKind.Identifier);
            this.Advance(TokenKind.AsKeyword);

            var parType = this.TopExpression();
            var parLoc = parName.Location.Span(parType.Location);

            if (!this.Peek(TokenKind.CloseParenthesis)) {
                this.Advance(TokenKind.Comma);
            }

            pars = pars.Add(new ParseFunctionParameter {
                Location = parLoc,
                Name = parName.Value,
                Type = parType
            });
        }

        var end = this.Advance(TokenKind.CloseParenthesis);
        var returnType = new VoidLiteral { Location = end.Location} as IParseExpression;

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
        var body = this.Block();      

        return new FunctionDeclaration(
            sig.Location.Span(body.Location), 
            sig,
            body);
    }
        
    private IDeclaration ExternFunctionDeclaration() {
        var start = this.Advance(TokenKind.ExternKeyword);
        var sig = this.FunctionSignature();
        var end = this.Advance(TokenKind.Semicolon);
        var loc = start.Location.Span(end.Location);

        return new ExternFunctionDeclaration {
            Location = loc,
            Signature = sig
        };
    }
        
    private IDeclaration StructDeclaration() {
        var start = this.Advance(TokenKind.StructKeyword);
        var name = this.Advance(TokenKind.Identifier).Value;
        var mems = new List<ParseStructMember>();

        this.Advance(TokenKind.OpenBrace);

        while (!this.Peek(TokenKind.CloseBrace)) {
            var memName = this.Advance(TokenKind.Identifier);
            this.Advance(TokenKind.AsKeyword);

            var memType = this.TopExpression();
            var memLoc = memName.Location.Span(memType.Location);

            this.Advance(TokenKind.Semicolon);
            mems.Add(new ParseStructMember {
                Location = memLoc,
                MemberName = memName.Value,
                MemberType = memType
            });
        }

        var last = this.Advance(TokenKind.CloseBrace);
        var loc = start.Location.Span(last.Location);

        var sig = new ParseStructSignature {
            Location = loc,
            Name = name,
            Members = mems
        };

        return new StructDeclaration {
            Location = loc,
            Signature = sig
        };
    }
        
    private IDeclaration UnionDeclaration() {
        var start = this.Advance(TokenKind.UnionKeyword);
        var name = this.Advance(TokenKind.Identifier).Value;
        var mems = new List<ParseStructMember>();

        this.Advance(TokenKind.OpenBrace);

        while (!this.Peek(TokenKind.CloseBrace)) {
            var memName = this.Advance(TokenKind.Identifier);
            this.Advance(TokenKind.AsKeyword);

            var memType = this.TopExpression();
            var memLoc = memName.Location.Span(memType.Location);

            this.Advance(TokenKind.Semicolon);
                
            mems.Add(new ParseStructMember {
                Location = memLoc,
                MemberName = memName.Value,
                MemberType = memType
            });
        }

        var last = this.Advance(TokenKind.CloseBrace);
        var loc = start.Location.Span(last.Location);

        var sig = new ParseStructSignature {
            Location = loc,
            Members = mems,
            Name = name
        };

        return new UnionDeclaration(loc, sig);
    }

    /** Statement Parsing **/
    private IParseStatement Statement() {
        IParseStatement result;

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
            result = this.BreakContinueStatement();
        }
        else if (this.Peek(TokenKind.ReturnKeyword)) {
            result = this.ReturnStatement();
        }
        else if (this.Peek(TokenKind.IfKeyword)) {
            result = this.IfStatement();
        }
        else if (this.Peek(TokenKind.VarKeyword)) {
            result = this.VariableStatement();
        }
        else {
            result = this.AssignmentStatement();
            this.Advance(TokenKind.Semicolon);
        }
            
        return result;
    }    
        
    private IParseStatement Block() {
        var start = this.Advance(TokenKind.OpenBrace);
        var stats = new List<IParseStatement>();

        while (!this.Peek(TokenKind.CloseBrace)) {
            stats.Add(this.Statement());
        }

        var end = this.Advance(TokenKind.CloseBrace);
        var loc = start.Location.Span(end.Location);

        return new BlockStatement {
            Location = loc,
            Statements = stats
        };
    }
        
    private IParseStatement WhileStatement() {
        var start = this.Advance(TokenKind.WhileKeyword);
        var cond = this.TopExpression();
            
        this.isInLoop.Push(true);
        var body = this.Block();
        this.isInLoop.Pop();
            
        var test = new IfStatement {
            Location = cond.Location,
            Condition = new UnaryExpression {
                Location = cond.Location,
                Operand = cond,
                Operator = UnaryOperatorKind.Not
            },
            Affirmative = new LoopControlStatement {
                Location = cond.Location,
                Kind = LoopControlKind.Break
            },
            Negative = new BlockStatement {
                Location = cond.Location,
                Statements = []
            }
        };

        var newBlock = new List<IParseStatement> {
            test,
            body,
        };

        var loc = start.Location.Span(body.Location);

        var loop = new LoopStatement {
            Location = loc,
            Body = new BlockStatement {
                Location = loc,
                Statements = newBlock
            }
        };

        return loop;
    }
        
    private IParseStatement BreakContinueStatement() {
        Token start;
        LoopControlKind kind;

        if (this.Peek(TokenKind.BreakKeyword)) {
            start = this.Advance(TokenKind.BreakKeyword);
            kind = LoopControlKind.Break;
        }
        else {
            start = this.Advance(TokenKind.ContinueKeyword);
            kind = LoopControlKind.Continue;
        }

        if (!this.isInLoop.Peek()) {
            throw new ParseException(
                start.Location, 
                "Invalid Statement", 
                "Break and continue statements must only appear inside of loops");
        }

        var end = this.Advance(TokenKind.Semicolon);
            
        return new LoopControlStatement {
            Location = start.Location.Span(end.Location),
            Kind = kind
        };
    }
    
    private IParseStatement ForStatement() {
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

        this.isInLoop.Push(true);
        var body = this.Block();
        this.isInLoop.Pop();

        var loc = startTok.Location.Span(body.Location);

        return new ForLoopStatement {
            Location = loc,
            VariableToken = id,
            StartValue = startIndex,
            EndValue = endIndex,
            IsInclusive = inclusive,
            Body = body
        };
    }
        
    private IParseStatement IfStatement() {
        var start = this.Advance(TokenKind.IfKeyword);
        var cond = this.TopExpression();
        var affirm = this.Block();

        if (this.TryAdvance(TokenKind.ElseKeyword)) {
            var neg = this.Block();
            var loc = start.Location.Span(neg.Location);

            return new IfStatement {
                Location = loc,
                Condition = cond,
                Affirmative = affirm,
                Negative = neg
            };
        }
        else {
            var loc = start.Location.Span(affirm.Location);

            return new IfStatement {
                Location = loc,
                Condition = cond,
                Affirmative = affirm,
                Negative = new BlockStatement {
                    Location = loc,
                    Statements = []
                }
            };
        }
    }
        
    private IParseStatement ReturnStatement() {
        var start = this.Advance(TokenKind.ReturnKeyword);
        var arg = this.TopExpression();
        var last = this.Advance(TokenKind.Semicolon);

        return new ReturnStatement {
            Location = start.Location.Span(last.Location),
            Operand = arg
        };
    }
        
    private IParseStatement AssignmentStatement() {
        var start = this.TopExpression();

        if (this.TryAdvance(TokenKind.Assignment)) {
            var assign = this.TopExpression();
            var loc = start.Location.Span(assign.Location);

            var result = new AssignmenStatement {
                Location = loc,
                Left = start,
                Right = assign
            };

            return result;
        }
        else {
            BinaryOperationKind op;

            // These are operators like += and -=
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
                return new ExpressionStatement {
                    Expression = start
                };
            }

            var second = this.TopExpression();
            var loc = start.Location.Span(second.Location);

            var assign = new BinaryExpression {
                Location = loc,
                Left = start,
                Right = second,
                Operator = op
            };

            var stat = new AssignmenStatement {
                Location = loc,
                Left = start,
                Right = assign
            };
            
            return stat;
        }
    }
        
    private IParseStatement VariableStatement() {
        var startLok = this.Advance(TokenKind.VarKeyword).Location;
        var names = new List<string>();
        var types = new List<Option<IParseExpression>>();

        while (true) {
            var name = this.Advance(TokenKind.Identifier).Value;
            names.Add(name);

            if (this.TryAdvance(TokenKind.AsKeyword)) {
                types.Add(Option.Some(this.TopExpression()));
            }
            else {
                types.Add(Option.None);
            }

            if (this.TryAdvance(TokenKind.Assignment)) {
                break;
            }
            else {
                this.Advance(TokenKind.Comma);
            }
        }

        var assign = this.TopExpression();
        var end = this.Advance(TokenKind.Semicolon);
        var loc = startLok.Span(end.Location);

        if (names.Count == 1) {
            return new VariableStatement {
                Location = loc,
                VariableName = names[0],
                VariableType = types[0],
                Assignment = assign
            };
        }
        else {
            return new MultiVariableStatement {
                Location = loc,
                VariableNames = names,
                VariableTypes = types,
                Assignment = assign
            };
        }
    }

        
    /** Expression Parsing **/
    private IParseExpression TopExpression() => this.BinaryExpression();

    private IParseExpression BinaryExpression() => this.OrExpression();

    private IParseExpression PrefixExpression() => this.AsExpression();     

    private IParseExpression SuffixExpression() {
        var first = this.Atom();

        while (this.Peek(TokenKind.OpenParenthesis) 
               || this.Peek(TokenKind.Dot) 
               || this.Peek(TokenKind.OpenBracket)) {

            if (this.Peek(TokenKind.OpenParenthesis)) {
                first = this.InvokeExpression(first);
            }
            else if (this.Peek(TokenKind.Dot)) {
                first = this.MemberAccessExpression(first);
            }
            else if (this.Peek(TokenKind.OpenBracket)) {
                first = this.ArrayExpression(first);
            }
            else {
                throw new Exception("Unexpected suffix token");
            }
        }

        return first;
    }        

    private IParseExpression Atom() {
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
        else if (this.Peek(TokenKind.WordKeyword)) {
            var tok = this.Advance(TokenKind.WordKeyword);

            return new TypeExpression {
                Location = tok.Location,
                Type = PrimitiveType.Word
            };
        }
        else if (this.Peek(TokenKind.BoolKeyword)) {
            var tok = this.Advance(TokenKind.BoolKeyword);

            return new TypeExpression {
                Location = tok.Location,
                Type = PrimitiveType.Bool
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
        
    private IParseExpression IfExpression() {
        var start = this.Advance(TokenKind.IfKeyword);
        var cond = this.TopExpression();

        this.Advance(TokenKind.ThenKeyword);
        var affirm = this.TopExpression();

        if (this.TryAdvance(TokenKind.ElseKeyword)) {
            var neg = this.TopExpression();
            var loc = start.Location.Span(neg.Location);

            return new IfExpression {
                Location = loc,
                Condition = cond,
                Affirmative = affirm,
                Negative = neg
            };
        }
        else {
            var loc = start.Location.Span(affirm.Location);

            return new IfExpression {
                Location = loc,
                Condition = cond,
                Affirmative = affirm,
                Negative = new VoidLiteral {
                    Location = loc
                }
            };
        }
    }

    private IParseExpression ParenExpression() {
        this.Advance(TokenKind.OpenParenthesis);
        var result = this.TopExpression();
        this.Advance(TokenKind.CloseParenthesis);

        return result;
    }
        
    public IParseExpression ArrayExpression(IParseExpression start) {
        this.Advance(TokenKind.OpenBracket);

        if (this.Peek(TokenKind.CloseBracket)) {
            var end = this.Advance(TokenKind.CloseBracket);
            var loc = start.Location.Span(end.Location);

            return new ArrayTypeExpression {
                Location = loc,
                Operand = start
            };
        }
        else {
            var index = this.TopExpression();
            var end = this.Advance(TokenKind.CloseBracket);
            var loc = start.Location.Span(end.Location);

            return new ArrayIndexExpression {
                Location = loc,
                Operand = start,
                Index = index
            };
        }            
    }
    
    private IParseExpression ArrayLiteral() {
        var start = this.Advance(TokenKind.OpenBracket);
        var args = new List<IParseExpression>();

        while (!this.Peek(TokenKind.CloseBracket)) {
            args.Add(this.TopExpression());

            if (!this.Peek(TokenKind.CloseBracket)) {
                this.Advance(TokenKind.Comma);
            }
        }

        var end = this.Advance(TokenKind.CloseBracket);
        var loc = start.Location.Span(end.Location);

        return new ArrayLiteral {
            Location = loc,
            Arguments = args
        };
    }
        
    private IParseExpression InvokeExpression(IParseExpression first) {
        this.Advance(TokenKind.OpenParenthesis);

        var args = new List<IParseExpression>();

        while (!this.Peek(TokenKind.CloseParenthesis)) {
            args.Add(this.TopExpression());

            if (!this.TryAdvance(TokenKind.Comma)) {
                break;
            }
        }

        var last = this.Advance(TokenKind.CloseParenthesis);
        var loc = first.Location.Span(last.Location);

        return new InvokeExpression {
            Location = loc,
            Operand = first,
            Arguments = args
        };
    }
        
    private IParseExpression UnaryExpression() {
        var hasOperator = this.Peek(TokenKind.Minus)
                          || this.Peek(TokenKind.Plus)
                          || this.Peek(TokenKind.Not)
                          || this.Peek(TokenKind.Ampersand)
                          || this.Peek(TokenKind.Star);

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
                return new AddressOfExpression {
                    Location = loc,
                    Operand = first
                };
            }
            else if (tokOp.Kind == TokenKind.Star) {
                return new DereferenceExpression {
                    Location = loc,
                    Operand = first
                };
            }
            else if (tokOp.Kind != TokenKind.Not) {
                throw new Exception("Unexpected unary operator");
            }

            return new UnaryExpression {
                Location = loc,
                Operand = first,
                Operator = op
            };
        }

        return this.SuffixExpression();
    }
        
    private IParseExpression WordLiteral() {
        var tok = this.Advance(TokenKind.WordLiteral);
        var num = long.Parse((string)tok.Value);

        return new WordLiteral {
            Location = tok.Location,
            Value = num
        };
    }
        
    private IParseExpression VoidLiteral() {
        var tok = this.Advance(TokenKind.VoidKeyword);

        return new VoidLiteral {
            Location = tok.Location
        };
    }
        
    private IParseExpression BoolLiteral() {
        var start = this.Advance(TokenKind.BoolLiteral);
        var value = bool.Parse((string)start.Value);

        return new BoolLiteral {
            Location = start.Location,
            Value = value
        };
    }
        
    private IParseExpression AsExpression() {
        var first = this.UnaryExpression();

        while (this.Peek(TokenKind.AsKeyword) || this.Peek(TokenKind.IsKeyword)) {
            if (this.TryAdvance(TokenKind.AsKeyword)) {
                var target = this.TopExpression();
                var loc = first.Location.Span(target.Location);

                first = new AsExpression {
                    Location = loc,
                    Operand = first,
                    TypeExpression = target
                };
            }
            else {
                this.Advance(TokenKind.IsKeyword);
                var nameTok = this.Advance(TokenKind.Identifier);

                first = new IsExpression {
                    Location = first.Location.Span(nameTok.Location),
                    Operand = first,
                    MemberName = nameTok.Value
                };
            }
        }

        return first;
    }
        
    private IParseExpression OrExpression() {
        var first = this.XorExpression();

        while (this.TryAdvance(TokenKind.OrKeyword)) {
            var branching = this.TryAdvance(TokenKind.ElseKeyword);
            var second = this.XorExpression();
            var loc = first.Location.Span(second.Location);

            if (branching) {
                first = new IfExpression {
                    Location = loc,
                    Condition = first,
                    Affirmative = new BoolLiteral {
                        Location = loc,
                        Value = true
                    },
                    Negative = second
                };
            }
            else {
                first = new BinaryExpression {
                    Location = loc,
                    Left = first,
                    Right = second,
                    Operator = BinaryOperationKind.Or
                };
            }
        }

        return first;
    }

    private IParseExpression XorExpression() {
        var first = this.AndExpression();

        while (this.TryAdvance(TokenKind.XorKeyword)) {
            var second = this.AndExpression();
            var loc = first.Location.Span(second.Location);

            first = new BinaryExpression {
                Location = loc,
                Left = first,
                Right = second,
                Operator = BinaryOperationKind.Xor
            };
        }

        return first;
    }

    private IParseExpression AndExpression() {
        var first = this.ComparisonExpression();

        while (this.TryAdvance(TokenKind.AndKeyword)) {
            var branching = this.TryAdvance(TokenKind.ThenKeyword);
            var second = this.XorExpression();
            var loc = first.Location.Span(second.Location);

            if (branching) {
                first = new IfExpression {
                    Location = loc,
                    Condition = new UnaryExpression {
                        Location = loc,
                        Operand = first,
                        Operator = UnaryOperatorKind.Not
                    },
                    Affirmative = new BoolLiteral {
                        Location = loc,
                        Value = false
                    },
                    Negative = second
                };
            }
            else {
                first = new BinaryExpression {
                    Location = loc,
                    Left = first,
                    Right = second,
                    Operator = BinaryOperationKind.And
                };
            }
        }

        return first;
    }

    private IParseExpression ComparisonExpression() {
        var first = this.AddExpression();
        var comparators = new Dictionary<TokenKind, BinaryOperationKind> {
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

            first = new BinaryExpression {
                Location = loc,
                Left = first,
                Right = second,
                Operator = op
            };
        }

        return first;
    }

    private IParseExpression AddExpression() {
        var first = this.MultiplyExpression();

        while (true) {
            if (!this.Peek(TokenKind.Plus) && !this.Peek(TokenKind.Minus)) {
                break;
            }

            var tok = this.Advance().Kind;
            var op = tok == TokenKind.Plus ? BinaryOperationKind.Add : BinaryOperationKind.Subtract;
            var second = this.MultiplyExpression();
            var loc = first.Location.Span(second.Location);

            first = new BinaryExpression {
                Location = loc,
                Left = first,
                Right = second,
                Operator = op
            }; 
        }

        return first;
    }

    private IParseExpression MultiplyExpression() {
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
                
            first = new BinaryExpression {
                Location = loc,
                Left = first,
                Right = second,
                Operator = op
            };
        }

        return first;
    }
        
    private IParseExpression NewExpression() {
        var start = this.Advance(TokenKind.NewKeyword).Location;
        var targetType = this.TopExpression();
        var loc = start.Span(targetType.Location);

        if (!this.TryAdvance(TokenKind.OpenBrace)) {
            return new NewExpression {
                Location = loc,
                TypeExpression = targetType
            };
        }

        var names = new List<string?>();
        var values = new List<IParseExpression>();

        while (!this.Peek(TokenKind.CloseBrace)) {
            string? name = null;

            if (this.Peek(TokenKind.Identifier)) {
                name = this.Advance(TokenKind.Identifier).Value;
                this.Advance(TokenKind.Assignment);
            }

            var value = this.TopExpression();

            names.Add(name);
            values.Add(value);

            if (!this.Peek(TokenKind.CloseBrace)) {
                this.Advance(TokenKind.Comma);
            }
        }

        var end = this.Advance(TokenKind.CloseBrace);
        loc = start.Span(end.Location);

        return new NewExpression {
            Location = loc,
            TypeExpression = targetType,
            Names = names,
            Values = values
        };
    }
        
    private IParseExpression MemberAccessExpression(IParseExpression first) {
        this.Advance(TokenKind.Dot);

        var tok = this.Advance(TokenKind.Identifier);
        var loc = first.Location.Span(tok.Location);

        return new MemberAccessExpression {
            Location = loc,
            Operand = first,
            MemberName = tok.Value
        };
    }
        
    private IParseExpression VariableAccess() {
        var tok = this.Advance(TokenKind.Identifier);

        return new VariableAccessExpression {
            Location = tok.Location,
            VariableName = tok.Value
        };
    }
}