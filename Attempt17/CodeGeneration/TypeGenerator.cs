using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.CodeGeneration {
    public class TypeGenerator : ITypeVisitor<string> {
        private readonly ICodeWriter headerWriter;
        private bool arrayGenerated = false;

        public TypeGenerator(ICodeWriter headerWriter) {
            this.headerWriter = headerWriter;
        }

        public string VisitArrayType(ArrayType type) {
            if (this.arrayGenerated) {
                return "$array";
            }

            this.headerWriter.Line("typedef struct $array {");
            this.headerWriter.Lines(CWriter.Indent("int64_t size;"));
            this.headerWriter.Lines(CWriter.Indent("uintptr_t data;"));
            this.headerWriter.Line("} $array;");
            this.headerWriter.EmptyLine();

            this.arrayGenerated = true;

            return "$array";
        }

        public string VisitBoolType(BoolType type) {
            return "uint16_t";
        }

        public string VisitIntType(IntType type) {
            return "int64_t";
        }

        public string VisitNamedType(NamedType type) {
            return type.Path.ToCName();
        }

        public string VisitVariableType(VariableType type) {
            return "uintptr_t";
        }

        public string VisitVoidType(VoidType type) {
            return "uint16_t";
        }
    }
}