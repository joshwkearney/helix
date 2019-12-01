using Attempt12.Analyzing;
using Attempt12.Interpreting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt12 {
    public class Interpreter : ISyntaxVisitor {
        private readonly ISyntax syntax;
        private readonly Stack<InterprativeScope> scopes = new Stack<InterprativeScope>();
        private readonly Stack<object> values = new Stack<object>();
        private readonly InterprativeScope initialScope;

        public Interpreter(AnalyzeResult result) {
            this.syntax = result.SyntaxTree;
            this.initialScope = result.InitialScope;
        }

        public object Interpret() {
            this.values.Clear();
            this.scopes.Clear();
            this.scopes.Push(this.initialScope);

            this.syntax.Accept(this);
            return this.values.Pop();
        }

        public void Visit(Int32Syntax syntax) {
            this.values.Push(syntax.Value);
        }

        public void Visit(Real32Syntax syntax) {
            this.values.Push(syntax.Value);
        }

        public void Visit(VariableReferenceSyntax syntax) {
            this.values.Push(this.scopes.Peek().Variables[syntax.Variable]);
        }

        public void Visit(VariableDeclarationSyntax syntax) {
            syntax.AssignmentExpression.Accept(this);
            object value = this.values.Peek();

            InterprativeScope scope = this.scopes
                .Peek()
                .AddVariable(syntax.Variable, value);

            this.scopes.Push(scope);
        }

        public void Visit(FunctionDefinitionSyntax syntax) {
            var closure = new Closure(syntax, this.scopes.Peek());
            this.values.Push(closure);
        }

        public void Visit(IntrinsicSyntax syntax) {
            List<object> args = new List<object>();

            foreach (var arg in syntax.IntrinsicArguments) {
                arg.Accept(this);
                args.Add(this.values.Pop());
            }

            if (syntax.IntrinsicKind == IntrinsicDescriptor.AddInt32) {
                this.values.Push((int)args[0] + (int)args[1]);
            }
            else if (syntax.IntrinsicKind == IntrinsicDescriptor.SubtractInt32) {
                this.values.Push((int)args[0] - (int)args[1]);
            }
            else if (syntax.IntrinsicKind == IntrinsicDescriptor.MultiplyInt32) {
                this.values.Push((int)args[0] * (int)args[1]);
            }
            else if (syntax.IntrinsicKind == IntrinsicDescriptor.DivideInt32) {
                this.values.Push((int)args[0] / (int)args[1]);
            }
            if (syntax.IntrinsicKind == IntrinsicDescriptor.AddReal32) {
                this.values.Push((float)args[0] + (float)args[1]);
            }
            else if (syntax.IntrinsicKind == IntrinsicDescriptor.SubtractReal32) {
                this.values.Push((float)args[0] - (float)args[1]);
            }
            else if (syntax.IntrinsicKind == IntrinsicDescriptor.MultiplyReal32) {
                this.values.Push((float)args[0] * (float)args[1]);
            }
            else if (syntax.IntrinsicKind == IntrinsicDescriptor.DivideReal32) {
                this.values.Push((float)args[0] / (float)args[1]);
            }
            else {
                throw new Exception("Unimplemented intrinsic function");
            }
        }

        public void Visit(FunctionInvokeSyntax syntax) {
            syntax.FunctionExpression.Accept(this);

            var function = (Closure)this.values.Pop();
            var scope = function.Scope;

            for (int i = 0; i < syntax.Arguments.Count; i++) {
                syntax.Arguments[i].Accept(this);

                scope = scope.AddVariable(
                    function.FunctionDefinition.Parameters[i], 
                    this.values.Pop()
                );
            }

            int scopeCount = this.scopes.Count;
            this.scopes.Push(scope);

            this.EnsureScopeCount(scopeCount, () => {
                function.FunctionDefinition.FunctionBody.Accept(this);
            });
        }

        public void Visit(StatementSyntax syntax) {
            int count = this.scopes.Count;

            syntax.StatementExpression.Accept(this);
            this.values.Pop();

            this.EnsureScopeCount(count, () => syntax.ReturnExpression.Accept(this));
        }

        private T EnsureScopeCount<T>(int count, Func<T> action) {
            T result = action();

            while (this.scopes.Count > count) {
                this.scopes.Pop();
            }

            return result;
        }

        private void EnsureScopeCount(int count, Action action) {
            action();

            while (this.scopes.Count > count) {
                this.scopes.Pop();
            }
        }
    }
}