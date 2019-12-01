using System;
using System.Collections.Generic;
using System.Text;
using LLVMSharp;

namespace Attempt7.Syntax {
    public class LlvmSymbol : ISymbol {
        public LanguageType ReturnType { get; }

        public LLVMValueRef Value { get; }

        public LlvmSymbol(LLVMValueRef value, LanguageType returnType) {
            this.Value = value;
            this.ReturnType = returnType;
        }

        public CompilationResult Compile(BuilderArgs compiler, Scope scope) {
            return new CompilationResult(this.Value, scope, this.ReturnType);
        }

        public InterpretationResult Interpret(Scope scope) {
            throw new NotSupportedException();
        }
    }
}