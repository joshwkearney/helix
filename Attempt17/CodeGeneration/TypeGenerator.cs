using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.CodeGeneration {
    public class TypeGenerator : ITypeVisitor<string> {
        private readonly ICodeWriter headerWriter;

        public TypeGenerator(ICodeWriter headerWriter) {
            this.headerWriter = headerWriter;
        }

        public string VisitIntType(IntType type) {
            return "long long";
        }

        public string VisitNamedType(NamedType type) {
            return type.Path.ToCName();
        }

        public string VisitVariableType(VariableType type) {
            return type.InnerType.Accept(this) + "*";
        }

        public string VisitVoidType(VoidType type) {
            return "short";
        }
    }
}