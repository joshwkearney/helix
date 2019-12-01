using System;
using System.Collections.Generic;
using System.Text;
using LLVMSharp;

namespace Attempt7.Syntax {
    public class LetExpression : ISymbol {
        public string VariableName { get; }

        public ISymbol AssignExpression { get; }

        public LetExpression(string name, ISymbol assign) {
            this.VariableName = name;
            this.AssignExpression = assign;
        }

        public CompilationResult Compile(BuilderArgs compiler, Scope scope) {
            var assign = this.AssignExpression.Compile(compiler, scope);
            scope = scope.WithVariable(this.VariableName, new LlvmSymbol(assign.Result, assign.ReturnType));

            return new CompilationResult(assign.Result, scope, assign.ReturnType);
        }

        public InterpretationResult Interpret(Scope scope) {
            var assign = this.AssignExpression.Interpret(scope);
            scope = scope.WithVariable(this.VariableName, assign.Result);

            return new InterpretationResult(assign.Result, scope, assign.ReturnType);
        }
    }
}