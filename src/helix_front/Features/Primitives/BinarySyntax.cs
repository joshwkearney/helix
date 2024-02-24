using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.FlowControl;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Predicates;
using System;
using Helix.HelixMinusMinus;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseTree OrExpression() {
            var first = this.XorExpression();

            while (this.TryAdvance(TokenKind.OrKeyword)) {
                var branching = this.TryAdvance(TokenKind.ElseKeyword);
                var second = this.XorExpression();
                var loc = first.Location.Span(second.Location);

                if (branching) {
                    first = new IfSyntax(
                        loc,
                        first,
                        new BoolLiteral(loc, true),
                        second);
                }
                else {
                    first = new BinarySyntax(loc, first, second, BinaryOperationKind.Or);
                }
            }

            return first;
        }

        private IParseTree XorExpression() {
            var first = this.AndExpression();

            while (this.TryAdvance(TokenKind.XorKeyword)) {
                var second = this.AndExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntax(loc, first, second, BinaryOperationKind.Xor);
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
                    first = new IfSyntax(
                        loc,
                        new UnaryParseSyntax(loc, UnaryOperatorKind.Not, first),
                        new BoolLiteral(loc, false),
                        second);
                }
                else {
                    first = new BinarySyntax(loc, first, second, BinaryOperationKind.And);
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

                first = new BinarySyntax(loc, first, second, op);
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

                first = new BinarySyntax(loc, first, second, op);
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

                first = new BinarySyntax(loc, first, second, op);
            }

            return first;
        }
    }
}

namespace Helix.Features.Primitives {
    public enum BinaryOperationKind {
        Add, Subtract, Multiply, Modulo, FloorDivide,
        And, Or, Xor,
        EqualTo, NotEqualTo,
        GreaterThan, LessThan,
        GreaterThanOrEqualTo, LessThanOrEqualTo
    }

    public record BinarySyntax(
        TokenLocation Location, 
        IParseTree Left, 
        IParseTree Right, 
        BinaryOperationKind Operation) : IParseTree {

        public ImperativeExpression ToImperativeSyntax(ImperativeSyntaxWriter writer) {
            var left = this.Left.ToImperativeSyntax(writer);
            var right = this.Right.ToImperativeSyntax(writer);
            var v = writer.GetTempVariable();
            var stat = new BinaryStatement(this.Location, v.Name, left, right, this.Operation);

            writer.AddStatement(stat);
            return ImperativeExpression.Variable(v);
        }
    }

    public record BinaryStatement(
        TokenLocation Location, 
        string ResultVariable, 
        ImperativeExpression Left, 
        ImperativeExpression Right, 
        BinaryOperationKind Operation) : IImperativeStatement {

        private static readonly Dictionary<BinaryOperationKind, HelixType> intOperations = new() {
            { BinaryOperationKind.Add,                  PrimitiveType.Word },
            { BinaryOperationKind.Subtract,             PrimitiveType.Word },
            { BinaryOperationKind.Multiply,             PrimitiveType.Word },
            { BinaryOperationKind.Modulo,               PrimitiveType.Word },
            { BinaryOperationKind.FloorDivide,          PrimitiveType.Word },
            { BinaryOperationKind.And,                  PrimitiveType.Word },
            { BinaryOperationKind.Or,                   PrimitiveType.Word },
            { BinaryOperationKind.Xor,                  PrimitiveType.Word },
            { BinaryOperationKind.EqualTo,              PrimitiveType.Bool },
            { BinaryOperationKind.NotEqualTo,           PrimitiveType.Bool },
            { BinaryOperationKind.GreaterThan,          PrimitiveType.Bool },
            { BinaryOperationKind.LessThan,             PrimitiveType.Bool },
            { BinaryOperationKind.GreaterThanOrEqualTo, PrimitiveType.Bool },
            { BinaryOperationKind.LessThanOrEqualTo,    PrimitiveType.Bool },
        };

        private static readonly Dictionary<BinaryOperationKind, HelixType> boolOperations = new() {
            { BinaryOperationKind.And,                  PrimitiveType.Bool },
            { BinaryOperationKind.Or,                   PrimitiveType.Bool },
            { BinaryOperationKind.Xor,                  PrimitiveType.Bool },
            { BinaryOperationKind.EqualTo,              PrimitiveType.Bool },
            { BinaryOperationKind.NotEqualTo,           PrimitiveType.Bool },
        };

        public void CheckTypes(TypeFrame types, ImperativeSyntaxWriter writer) {
            var leftType = this.Left.GetReturnType(types);
            var rightType = this.Right.GetReturnType(types);

            if (leftType.IsBool(types) && rightType.IsBool(types)) {
                this.CheckBoolExpresion(this.Left, this.Right, types, writer);
                return;
            }
            else if (leftType.IsInt(types) && rightType.IsInt(types)) {
                this.CheckIntExpresion(this.Left, this.Right, types, writer);
                return;
            }

            var left = this.Left.UnifyFrom(this.Right, types, writer);
            var right = this.Right.UnifyFrom(this.Left, types, writer);

            if (left.GetReturnType(types).IsBool(types) && right.GetReturnType(types).IsBool(types)) {
                this.CheckBoolExpresion(left, right, types, writer);
                return;
            }
            else if (left.GetReturnType(types).IsInt(types) && right.GetReturnType(types).IsInt(types)) {
                this.CheckIntExpresion(left, right, types, writer);
                return;
            }

            throw TypeException.UnexpectedType(this.Right.Location, left.GetReturnType(types));
        }

        private void CheckIntExpresion(ImperativeExpression left, ImperativeExpression right, 
                                       TypeFrame types, ImperativeSyntaxWriter writer) {

            if (!intOperations.TryGetValue(this.Operation, out var generalReturnType)) {
                throw TypeException.UnexpectedType(this.Left.Location, left.GetReturnType(types));
            }

            var leftType = left.GetReturnType(types);
            var rightType = right.GetReturnType(types);

            if (leftType is SingularWordType singLeft && rightType is SingularWordType singRight) {
                var value = this.EvaluateIntExpression(singLeft.Value, singRight.Value, types);

                types.Locals = types.Locals.SetItem(this.ResultVariable, new LocalInfo(value));
            }
            else {
                var result = new BinaryStatement(this.Location, this.ResultVariable, left, right, this.Operation);

                types.Locals = types.Locals.SetItem(this.ResultVariable, new LocalInfo(generalReturnType));
                writer.AddStatement(result);
            }
        }

        private HelixType EvaluateIntExpression(long int1, long int2, TypeFrame types) {
            switch (this.Operation) {
                case BinaryOperationKind.Add:
                    return new SingularWordType(int1 + int2);
                case BinaryOperationKind.Subtract:
                    return new SingularWordType(int1 - int2);
                case BinaryOperationKind.Modulo:
                    return new SingularWordType(int1 % int2);
                case BinaryOperationKind.FloorDivide:
                    return new SingularWordType(int1 / int2);
                case BinaryOperationKind.And:
                    return new SingularWordType(int1 & int2);
                case BinaryOperationKind.Or:
                    return new SingularWordType(int1 | int2);
                case BinaryOperationKind.Xor:
                    return new SingularWordType(int1 ^ int2);
                case BinaryOperationKind.EqualTo:
                    return new SingularBoolType(int1 == int2);
                case BinaryOperationKind.NotEqualTo:
                    return new SingularBoolType(int1 != int2);
                case BinaryOperationKind.GreaterThan:
                    return new SingularBoolType(int1 > int2);
                case BinaryOperationKind.LessThan:
                    return new SingularBoolType(int1 < int2);
                case BinaryOperationKind.GreaterThanOrEqualTo:
                    return new SingularBoolType(int1 >= int2);
                case BinaryOperationKind.LessThanOrEqualTo:
                    return new SingularBoolType(int1 <= int2);
                default:
                    throw new InvalidOperationException();
            }
        }

        private void CheckBoolExpresion(ImperativeExpression left, ImperativeExpression right, 
                                        TypeFrame types, ImperativeSyntaxWriter writer) {

            var leftType = left.GetReturnType(types);
            var rightType = right.GetReturnType(types);

            if (!boolOperations.TryGetValue(this.Operation, out _)) {
                throw TypeException.UnexpectedType(this.Left.Location, leftType);
            }

            var predicate = ISyntaxPredicate.Empty;
            var returnType = PrimitiveType.Bool as HelixType;

            if (leftType is PredicateBool leftPred && rightType is PredicateBool rightPred) {
                switch (this.Operation) {
                    case BinaryOperationKind.And:
                        predicate = leftPred.Predicate.And(rightPred.Predicate);
                        break;
                    case BinaryOperationKind.Or:
                        predicate = leftPred.Predicate.Or(rightPred.Predicate);
                        break;
                    case BinaryOperationKind.NotEqualTo:
                    case BinaryOperationKind.Xor:
                        predicate = leftPred.Predicate.Xor(rightPred.Predicate);
                        break;
                    case BinaryOperationKind.EqualTo:
                        predicate = leftPred.Predicate.Xor(rightPred.Predicate).Negate();
                        break;
                }

                returnType = new PredicateBool(predicate);
            }

            if (leftType is SingularBoolType singLeft && rightType is SingularBoolType singRight) {
                var value = this.EvaluateBoolExpression(singLeft.Value, singRight.Value, predicate);

                types.Locals = types.Locals.SetItem(this.ResultVariable, new LocalInfo(value));
            }
            else {
                var result = new BinaryStatement(this.Location, this.ResultVariable, left, right, this.Operation);

                types.Locals = types.Locals.SetItem(this.ResultVariable, new LocalInfo(returnType));
                writer.AddStatement(result);
            }
        }

        private HelixType EvaluateBoolExpression(bool b1, bool b2, ISyntaxPredicate pred) {
            bool value;

            switch (this.Operation) {
                case BinaryOperationKind.And:
                    value = b1 & b2;
                    break;
                case BinaryOperationKind.Or:
                    value = b1 | b2;
                    break;
                case BinaryOperationKind.NotEqualTo:
                case BinaryOperationKind.Xor:
                    value = b1 ^ b2;
                    break;
                case BinaryOperationKind.EqualTo:
                    value = b1 == b2;
                    break;
                default:
                    throw new Exception();
            }

            return new SingularBoolType(value, pred);            
        }

        public string[] Write() {
            var op = this.Operation switch {
                BinaryOperationKind.Add => "+",
                BinaryOperationKind.And => "&",
                BinaryOperationKind.EqualTo => "==",
                BinaryOperationKind.GreaterThan => ">",
                BinaryOperationKind.GreaterThanOrEqualTo => ">=",
                BinaryOperationKind.LessThan => "<",
                BinaryOperationKind.LessThanOrEqualTo => "<=",
                BinaryOperationKind.Multiply => "*",
                BinaryOperationKind.NotEqualTo => "!=",
                BinaryOperationKind.Or => "|",
                BinaryOperationKind.Subtract => "-",
                BinaryOperationKind.Xor => "^",
                BinaryOperationKind.Modulo => "%",
                BinaryOperationKind.FloorDivide => "/",
                _ => throw new Exception()
            };

            return new[] { $"var {this.ResultVariable} = {this.Left} {op} {this.Right};" };
        }
    }
}