using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt7 {
    public abstract class LanguageType : ISymbol {
        public static LanguageType Int32Type { get; } = new LanguageType<int>(LLVM.Int32Type());

        public abstract Type ClrType { get; }

        public abstract LLVMTypeRef LlvmType { get; }

        public CompilationResult Compile(BuilderArgs compiler, Scope scope) {
            throw new NotSupportedException();
        }

        public InterpretationResult Interpret(Scope scope) {
            throw new NotSupportedException();
        }

        public static bool operator ==(LanguageType lang1, LanguageType lang2) {
            return lang1.Equals(lang2);
        }

        public static bool operator !=(LanguageType lang1, LanguageType lang2) {
            return !lang1.Equals(lang2);
        }
    }

    public class LanguageType<T> : LanguageType, IEquatable<LanguageType<T>> {
        public override Type ClrType => typeof(T);

        public override LLVMTypeRef LlvmType { get; }

        public LanguageType(LLVMTypeRef llvmType) {
            this.LlvmType = llvmType;
        }

        public override bool Equals(object obj) {
            if (obj is LanguageType<T> lang) {
                return this.Equals(lang);
            }
            else {
                return false;
            }
        }

        public override int GetHashCode() {
            int hashCode = 1904358260;
            hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(this.ClrType);
            hashCode = hashCode * -1521134295 + EqualityComparer<LLVMTypeRef>.Default.GetHashCode(this.LlvmType);
            return hashCode;
        }

        public bool Equals(LanguageType<T> other) {
            return this.ClrType == other.ClrType && this.LlvmType.Pointer == other.LlvmType.Pointer;
        }
    }
}