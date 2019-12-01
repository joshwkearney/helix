using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt6.Syntax {
    public interface ILanguageType {
        LLVMTypeRef LlvmType { get; }
    }

    public class PrimitiveType : ILanguageType {
        public static PrimitiveType Int32Type { get; } = new PrimitiveType(LLVM.Int32Type());

        public LLVMTypeRef LlvmType { get; }

        public PrimitiveType(LLVMTypeRef type) {
            this.LlvmType = type;
        }
    }

    public class FunctionType : ILanguageType {
        private Lazy<LLVMTypeRef> type;

        public ILanguageType ReturnType { get; }

        public IReadOnlyList<ILanguageType> ParameterTypes { get; }

        public LLVMTypeRef LlvmType => this.type.Value;

        public FunctionType(ILanguageType returnType, params ILanguageType[] paramTypes) {
            this.ReturnType = returnType;
            this.ParameterTypes = paramTypes;

            this.type = new Lazy<LLVMTypeRef>(() => 
                LLVM.FunctionType(
                    this.ReturnType.LlvmType, 
                    this.ParameterTypes.Select(x => x.LlvmType).ToArray(), 
                    false
                )
            );
        }
    }
}