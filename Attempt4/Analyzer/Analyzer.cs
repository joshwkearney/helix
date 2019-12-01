using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attempt4 {
    public class Analyzer : ISyntaxVisitor {
        private readonly Stack<IAnalyzedSyntax> values = new Stack<IAnalyzedSyntax>();
        private readonly Stack<Scope> scopes = new Stack<Scope>();
        private readonly ISyntaxTree tree;

        public Analyzer(ISyntaxTree tree) {
            this.tree = tree;
        }

        public IAnalyzedSyntax Analyze() {
            this.values.Clear();
            this.scopes.Clear();

            Scope rootScope = new Scope(new Dictionary<string, IAnalyzedSyntax>() {
                { "AddInt32", IntrinsicFunction.AddInt32 },
                { "SubtractInt32", IntrinsicFunction.SubtractInt32 },
                { "MultiplyInt32", IntrinsicFunction.MultiplyInt32 },
                { "DivideInt32", IntrinsicFunction.DivideInt32 }
            });

            this.scopes.Push(rootScope);
            this.tree.Accept(this);

            return this.values.Pop();
        }

        public void Visit(IntegerLiteral literal) {
            this.values.Push(literal);
        }

        public void Visit(FunctionCallExpression expr) {
            expr.Target.Accept(this);
            var target = this.values.Pop();

            if (!(target is IntrinsicFunction func)) {
                throw new CompileException(CompileExceptionCategory.Semantic, 0, "Cannot invoke non invokable value");
            }

            if (expr.Arguments.Count != func.Parameters.Count) {
                throw new CompileException(CompileExceptionCategory.Semantic, 0, $"Amount of arguments and parameters of function do not match");
            }

            var args = new List<IAnalyzedSyntax>();

            for (int i = 0; i < expr.Arguments.Count; i++) {
                expr.Arguments[i].Accept(this);
                var arg = this.values.Pop();

                if (arg.ExpressionType != func.Parameters[i].Type) {
                    throw new CompileException(CompileExceptionCategory.Semantic, 0, "Arguments and parameters of funcion to not have matching types");
                }

                args.Add(arg);
            }

            this.values.Push(new FunctionCall(func, args));
        }

        public void Visit(IdentifierLiteral literal) {
            if (!this.scopes.Peek().Symbols.TryGetValue(literal.Value, out var value)) {
                throw new CompileException(CompileExceptionCategory.Semantic, 0, $"Identifier '{literal.Value}' is undefined");
            }

            this.values.Push(value);
        }
    }
}