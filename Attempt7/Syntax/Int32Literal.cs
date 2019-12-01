using System;
using System.Collections.Generic;
using System.Text;
using LLVMSharp;

namespace Attempt7.Syntax {
    public class Int32Literal : IPrimitiveSymbol {
        public int Value { get; }

        object IPrimitiveSymbol.Value => this.Value;

        public LanguageType ReturnType => LanguageType.Int32Type;

        public Int32Literal(int value) {
            this.Value = value;
        }

        public object Interpret(Scope scope) {
            return this.Value;
        }

        public CompilationResult Compile(BuilderArgs compiler, Scope scope) {
            return new CompilationResult(
                LLVM.ConstInt(LanguageType.Int32Type.LlvmType, (ulong)this.Value, false),
                scope,
                LanguageType.Int32Type
            );
        }

        InterpretationResult ISymbol.Interpret(Scope scope) => new InterpretationResult(this, scope, LanguageType.Int32Type);

        public override string ToString() {
            return this.Value.ToString();
        }
    }
}