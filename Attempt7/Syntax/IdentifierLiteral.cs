using System;
using System.Collections.Generic;
using System.Text;
using LLVMSharp;

namespace Attempt7.Syntax {
    public class IdentifierLiteral : ISymbol {
        public string VariableName { get; }

        public IdentifierLiteral(string name) {
            this.VariableName = name;
        }

        public CompilationResult Compile(BuilderArgs compiler, Scope scope) {
            return scope.Variables[this.VariableName].Compile(compiler, scope);
        }

        public InterpretationResult Interpret(Scope scope) {
            return scope.Variables[this.VariableName].Interpret(scope);
        }
    }
}