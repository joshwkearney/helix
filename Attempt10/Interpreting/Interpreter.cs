using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt12 {
    public class Closure {
        public ImmutableDictionary<string, InterpretedValue> Environment { get; }

        public FunctionLiteralSyntax Function { get; }

        public Scope BodyScope { get; }

        public Closure(FunctionLiteralSyntax func, ImmutableDictionary<string, InterpretedValue> env) {
            this.Function = func;
            this.Environment = env;

            var bodyScope = func.Scope.IncrementClosureLevel().SetEnclosedFunctionType(func.ExpressionType);
            var infos = func
                .ClosedVariables
                .Concat(func.Parameters.Select(x => new VariableInfo(x.Name, x.Type, func.Scope.ClosureLevel + 1)))
                .ToArray();

            foreach (var info in infos) {
                bodyScope = bodyScope.SetVariable(info.Name, info.Type, info.DefinedClosureLevel);
            }

            bodyScope = bodyScope.IncrementClosureLevel();

            this.BodyScope = bodyScope;
        }
    }

    public class Interpreter : ISyntaxTreeVisitor {
        private readonly Stack<ImmutableDictionary<string, InterpretedValue>> variables = new Stack<ImmutableDictionary<string, InterpretedValue>>();
        private readonly Stack<InterpretedValue> values = new Stack<InterpretedValue>();
        private readonly Stack<Closure> currentFunction = new Stack<Closure>();

        public InterpretedValue Interpret(ISyntaxTree tree) {
            this.variables.Clear();
            this.values.Clear();

            this.variables.Push(ImmutableDictionary<string, InterpretedValue>.Empty);

            tree.Accept(this);
            return this.values.Pop();
        }             

        public void Visit(Int64LiteralSyntax value) {
            this.values.Push(
                new ConstantInterpretedValue(
                    value.Value, 
                    new Int64LiteralSyntax(value.Value, value.Scope)));
        }

        public void Visit(VariableLiteralSyntax value) {
            this.values.Push(this.variables.Peek()[value.Name]);
        }

        public void Visit(IfExpressionSyntax value) {
            value.Condition.Accept(this);
            
            if ((bool)this.values.Pop().Value) {
                value.AffirmativeExpression.Accept(this);
            }
            else {
                value.NegativeExpression.Accept(this);
            }
        }

        public void Visit(VariableDefinitionSyntax value) {
            value.AssignExpression.Accept(this);
            var assign = this.values.Pop();

            this.variables.Push(this.variables.Peek().SetItem(value.Name, assign));
            value.ScopeExpression.Accept(this);
            this.variables.Pop();
        }

        public void Visit(FunctionLiteralSyntax value) {
            var builder = ImmutableDictionary.CreateBuilder<string, InterpretedValue>();

            foreach (var closed in value.ClosedVariables) {
                builder.Add(closed.Name, this.variables.Peek()[closed.Name]);
            }

            InterpretedValue result;
            var closure = new Closure(value, builder.ToImmutableDictionary());

            if (value.IsConstant) {
                result = new ConstantInterpretedValue(closure, value);
            }
            else {
                result = new InterpretedValue(closure);
            }

            this.values.Push(result);
        }

        public void Visit(FunctionInvokeSyntax value) {
            value.Target.Accept(this);
            var func = (Closure)this.values.Pop().Value;

            var args = value.Arguments.Select(x => {
                x.Accept(this);
                return this.values.Pop();
            })
            .ToArray();

            var funcType = (TrophyFunctionType)value.Target.ExpressionType;
            var argPairs = func
                .Function
                .Parameters
                .Zip(args, (x, y) => (name: x.Name, value: y))
                .ToDictionary(x => x.name, x => x.value);            

            var variables = this.variables.Peek().SetItems(func.Environment);
            variables = variables.SetItems(argPairs);

            this.variables.Push(variables);
            this.currentFunction.Push(func);

            func.Function.Body.Accept(this);

            this.variables.Pop();
            this.currentFunction.Pop();
        }

        public void Visit(BoolLiteralSyntax value) {
            this.values.Push(
                new ConstantInterpretedValue(
                    value.Value,
                    new BoolLiteralSyntax(value.Value, value.Scope)));
        }

        public void Visit(Real64Literal value) {
            this.values.Push(
                new ConstantInterpretedValue(
                    value.Value,
                    new Real64Literal(value.Value, value.Scope)));
        }

        public void Visit(PrimitiveOperationSyntax value) {
            var ops = value.Operands.Select(x => {
                x.Accept(this);
                return this.values.Pop();
            })
            .Select(x => x.Value)
            .ToArray();

            void pushInt64UnaryOp(Func<long, long> func) {
                long result = func((long)ops[0]);
                this.values.Push(
                    new ConstantInterpretedValue(
                        result,
                        new Int64LiteralSyntax(result, value.Scope)));
            }

            void pushInt64BinaryOp(Func<long, long, long> func) {
                long result = func((long)ops[0], (long)ops[1]);
                this.values.Push(
                    new ConstantInterpretedValue(
                        result,
                        new Int64LiteralSyntax(result, value.Scope)));
            }

            void pushReal64UnaryOp(Func<double, double> func) {
                double result = func((double)ops[0]);
                this.values.Push(
                    new ConstantInterpretedValue(
                        result,
                        new Real64Literal(result, value.Scope)));
            }

            void pushReal64BinaryOp(Func<double, double, double> func) {
                double result = func((double)ops[0], (double)ops[1]);
                this.values.Push(
                    new ConstantInterpretedValue(
                        result,
                        new Real64Literal(result, value.Scope)));
            }

            void pushBooleanUnaryOp(Func<bool, bool> func) {
                bool result = func((bool)ops[0]);
                this.values.Push(
                    new ConstantInterpretedValue(
                        result,
                        new BoolLiteralSyntax(result, value.Scope)));
            }

            void pushBooleanBinaryOp(Func<bool, bool, bool> func) {
                bool result = func((bool)ops[0], (bool)ops[1]);
                this.values.Push(
                    new ConstantInterpretedValue(
                        result,
                        new BoolLiteralSyntax(result, value.Scope)));
            }

            switch (value.Operation) {
                case PrimitiveOperation.Int64Not:
                    pushInt64UnaryOp(x => ~x);
                    return;
                case PrimitiveOperation.Int64Negate:
                    pushInt64UnaryOp(x => -x);
                    return;
                case PrimitiveOperation.Int64Add:
                    pushInt64BinaryOp((x, y) => x + y);
                    return;
                case PrimitiveOperation.Int64Subtract:
                    pushInt64BinaryOp((x, y) => x - y);
                    return;
                case PrimitiveOperation.Int64Multiply:
                    pushInt64BinaryOp((x, y) => x * y);
                    return;
                case PrimitiveOperation.Int64RealDivide:
                    double result = (long)ops[0] * 1d / (long)ops[1];
                    this.values.Push(
                        new ConstantInterpretedValue(
                            result,
                            new Real64Literal(result, value.Scope)));
                    return;
                case PrimitiveOperation.Int64StrictDivide:
                    pushInt64BinaryOp((x, y) => x / y);
                    return;
                case PrimitiveOperation.Int64And:
                    pushInt64BinaryOp((x, y) => x & y);
                    return;
                case PrimitiveOperation.Int64Or:
                    pushInt64BinaryOp((x, y) => x | y);
                    return;
                case PrimitiveOperation.Int64Xor:
                    pushInt64BinaryOp((x, y) => x ^ y);
                    return;
                case PrimitiveOperation.Int64GreaterThan:
                    bool boolResult = (long)ops[0] > (long)ops[1];
                    this.values.Push(
                        new ConstantInterpretedValue(
                            boolResult,
                            new BoolLiteralSyntax(boolResult, value.Scope)));
                    return;
                case PrimitiveOperation.Int64LessThan:
                    boolResult = (long)ops[0] < (long)ops[1];
                    this.values.Push(
                        new ConstantInterpretedValue(
                            boolResult,
                            new BoolLiteralSyntax(boolResult, value.Scope)));
                    return;
                case PrimitiveOperation.Real64Negate:
                    pushReal64UnaryOp(x => -x);
                    return;
                case PrimitiveOperation.Real64Add:
                    pushReal64BinaryOp((x, y) => x + y);
                    return;
                case PrimitiveOperation.Real64Subtract:
                    pushReal64BinaryOp((x, y) => x - y);
                    return;
                case PrimitiveOperation.Real64Multiply:
                    pushReal64BinaryOp((x, y) => x * y);
                    return;
                case PrimitiveOperation.Real64Divide:
                    pushReal64BinaryOp((x, y) => x / y);
                    return;
                case PrimitiveOperation.Real64GreaterThan:
                    boolResult = (double)ops[0] > (double)ops[1];
                    this.values.Push(
                        new ConstantInterpretedValue(
                            boolResult,
                            new BoolLiteralSyntax(boolResult, value.Scope)));
                    return;
                case PrimitiveOperation.Real64LessThan:
                    boolResult = (double)ops[0] < (double)ops[1];
                    this.values.Push(
                        new ConstantInterpretedValue(
                            boolResult,
                            new BoolLiteralSyntax(boolResult, value.Scope)));
                    return;
                case PrimitiveOperation.BooleanNot:
                    pushBooleanUnaryOp(x => !x);
                    return;
                case PrimitiveOperation.BooleanAnd:
                    pushBooleanBinaryOp((x, y) => x && y);
                    return;
                case PrimitiveOperation.BooleanOr:
                    pushBooleanBinaryOp((x, y) => x || y);
                    return;
                case PrimitiveOperation.BooleanXor:
                    pushBooleanBinaryOp((x, y) => x ^ y);
                    return;
            }

            throw new Exception();
        }

        public void Visit(FunctionRecurseLiteral value) {
            this.values.Push(new InterpretedValue(this.currentFunction.Peek()));
        }
    }
}