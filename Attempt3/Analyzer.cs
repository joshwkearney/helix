using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt3 {
    public class Analyzer : ISyntaxVisitor {
        private readonly ISyntaxTree tree;
        private readonly Stack<Scope> scopes = new Stack<Scope>();
        private readonly Stack<IValue> values = new Stack<IValue>();

        public Analyzer(ISyntaxTree tree) {
            this.tree = tree;
        }

        public IValue Analyze() {
            Scope sc = new Scope();
            sc = new Scope(new Dictionary<string, IValue>() {
                { "AddInt32", new IntrinsicLiteral("AddInt32") },
                { "SubtractInt32", new IntrinsicLiteral("SubtractInt32") },
                { "MultiplyInt32", new IntrinsicLiteral("MultiplyInt32") },
                { "DivideInt32", new IntrinsicLiteral("DivideInt32") }
            });

            this.scopes.Clear();
            this.scopes.Push(sc);

            this.values.Clear();
            this.tree.Accept(this);

            return this.values.Pop();
        }

        public void Visit(FunctionCallExpression expr) {
            // TODO - Check invokablility

            expr.Target.Accept(this);
            var target = this.values.Pop();

            var args = new List<IValue>();
            foreach (var ast in expr.Arguments) {
                ast.Accept(this);
                args.Add(this.values.Pop());
            }

            this.values.Push(new FunctionCall(target, args.ToArray()));
        }

        public void Visit(FunctionLiteral expr) {
            throw new NotImplementedException();
        }

        public void Visit(IdentifierLiteral leaf) {
            if (!this.scopes.Peek().Symbols.TryGetValue(leaf.VariableName, out var value)) {
                throw new CompileException(CompileExceptionCategory.Semantic, 0, $"Scope does not contain variable {leaf.VariableName}");
            }

            this.values.Push(value);
        }

        public void Visit(IntegerLiteral leaf) {
            this.values.Push(leaf);
        }
    }
}