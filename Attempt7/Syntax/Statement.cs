using System;
using System.Collections.Generic;
using System.Text;
using LLVMSharp;

namespace Attempt7.Syntax {
    public class Statement : ISymbol {
        public ISymbol StatementExpression { get; }

        public ISymbol ScopeExpression { get; }

        public Statement(ISymbol stat, ISymbol scope) {
            this.StatementExpression = stat;
            this.ScopeExpression = scope;
        }

        public CompilationResult Compile(BuilderArgs compiler, Scope scope) {
            var result = this.StatementExpression.Compile(compiler, scope);
            return this.ScopeExpression.Compile(compiler, result.LastScope);
        }

        public InterpretationResult Interpret(Scope scope) {
            var result = this.StatementExpression.Interpret(scope);
            return this.ScopeExpression.Interpret(result.LastScope);
        }
    }
}