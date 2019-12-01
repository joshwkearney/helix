using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LLVMSharp;

namespace Attempt7 {
    public class Closure : IPrimitiveSymbol {
        public Func<IReadOnlyList<ISymbol>, ISymbol> Function { get; }

        object IPrimitiveSymbol.Value => this.Function;

        public CompilationResult Compile(BuilderArgs compiler, Scope scope) {
            throw new notsu();
        }

        public InterpretationResult Interpret(Scope scope) {
            throw new NotImplementedException();
        }

        CompilationResult ISymbol.Compile(BuilderArgs compiler, Scope scope) {
            throw new NotImplementedException();
        }

        InterpretationResult ISymbol.Interpret(Scope scope) {
            throw new NotImplementedException();
        }
    }

    public class ClosureType : LanguageType, IEquatable<ClosureType> {
        public LanguageType ReturnType { get; }

        public IReadOnlyList<LanguageType> ParameterTypes { get; }

        public override Type ClrType => typeof(Closure);

        public override LLVMTypeRef LlvmType { get; }

        public ClosureType(LanguageType returnType, IReadOnlyList<LanguageType> paramTypes) {
            this.ReturnType = returnType;
            this.ParameterTypes = paramTypes;

            this.LlvmType = LLVM.FunctionType(
                this.ReturnType.LlvmType,
                this.ParameterTypes.Select(x => x.LlvmType).ToArray(), 
                false
            );
        }

        public bool Equals(ClosureType other) {
            return this.ReturnType.Equals(other.ReturnType) && this.ParameterTypes.SequenceEqual(other.ParameterTypes);
        }

        public override bool Equals(object obj) {
            if (obj is ClosureType type) {
                return this.Equals(type);
            }
            else {
                return false;
            }
        }
    }
}