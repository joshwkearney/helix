using Attempt6.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt6.Evaluating {
    public class Evaluator : IASTVisitor {
        private readonly IAST tree;
        private readonly Stack<object> values = new Stack<object>();
        private Stack<IReadOnlyDictionary<string, object>> scopes = new Stack<IReadOnlyDictionary<string, object>>();

        public Evaluator(IAST tree, IReadOnlyDictionary<string, object> builtins) {
            this.tree = tree;
            this.scopes.Push(builtins);
        }

        public object Evalutate() {
            this.tree.Accept(this);
            return this.values.Pop();
        }

        public void Visit(IfExpression expr) {
            expr.Condition.Accept(this);
            int? condition = this.values.Pop() as int?;

            if (condition == null) {
                throw new Exception();
            }

            if (condition.Value != 0) {
                expr.IfTrue.Accept(this);
            }
            else {
                expr.IfFalse.Accept(this);
            }
        }

        public void Visit(LetExpression expr) {
            expr.AssignExpression.Accept(this);
            object assign = this.values.Pop();

            var nScope = this.scopes.Peek().ToDictionary(x => x.Key, x => x.Value);
            nScope.Add(expr.Name, assign);

            this.scopes.Push(nScope);
            expr.ScopeExpression.Accept(this);
            this.scopes.Pop();
        }

        public void Visit(FunctionCallExpression expr) {
            expr.InvokeTarget.Accept(this);
            object result = this.values.Pop();

            List<object> args = new List<object>();
            foreach (var tree in expr.Arguments) {
                tree.Accept(this);
                args.Add(this.values.Pop());
            }

            if (result is Func<IReadOnlyList<object> , object> func) {
                this.values.Push(func.Invoke(args));
            }
            else {
                throw new Exception();
            }
        }

        public void Visit(IdentifierLiteral expr) {
            this.values.Push(this.scopes.Peek()[expr.Name]);
        }

        public void Visit(Int32Literal expr) {
            this.values.Push(expr.Value);
        }

        public void Visit(FunctionDeclaration expr) {
            var currentScope = this.scopes.Peek().ToDictionary(x => x.Key, x => x.Value);

            Func<IReadOnlyList<object>, object> func = args => {
                var scope = currentScope.ToDictionary(x => x.Key, x => x.Value);

                if (args.Count != expr.Parameters.Count) {
                    throw new Exception();
                }

                var argPairs = expr.Parameters.Zip(args, (x, y) => (name: x, value: y));

                foreach (var pair in argPairs) {
                    scope[pair.name] = pair.value;
                }

                this.scopes.Push(scope);
                expr.Body.Accept(this);
                object ret = this.values.Pop();
                this.scopes.Pop();

                return ret;
            };

            currentScope.Add("recurse", func);
            this.values.Push(func);
        }
    }
}