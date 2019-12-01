using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attempt2.Compiling {
    public interface ISymbol { }

    public interface IValueSymbol  : ISymbol {
        ITypeSymbol TypeSymbol { get; }
        LLVMValueRef Value { get; }
    }
    
    public interface ITypeSymbol : ISymbol {
        LLVMTypeRef DefinedType { get; }
    }

    public class PrimitiveTypeSymbol : ITypeSymbol {
        public LLVMTypeRef DefinedType { get; }

        public PrimitiveTypeSymbol(LLVMTypeRef type) {
            this.DefinedType = type;
        }
    }

    public class PrimitiveValueSymbol : IValueSymbol {
        public ITypeSymbol TypeSymbol { get; }

        public LLVMValueRef Value { get; }

        public PrimitiveValueSymbol(LLVMValueRef value, ITypeSymbol primitiveType) {
            this.TypeSymbol = primitiveType;
            this.Value = value;
        }
    }
}