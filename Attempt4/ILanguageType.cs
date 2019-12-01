using LLVMSharp;
using System;
using System.Text;

namespace Attempt4 {
    public struct FunctionParameter {
        public LanguageType Type { get; }

        public string Name { get; }

        public FunctionParameter(LanguageType type, string name) {
            this.Type = type;
            this.Name = name;
        }
    }

    public class LanguageType {
        public static LanguageType Int32Type { get; } = new LanguageType(LLVM.Int32Type());

        LLVMTypeRef CompiledType { get; }

        public LanguageType(LLVMTypeRef type) {
            this.CompiledType = type;
        }
    }
}