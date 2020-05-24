using Attempt19.Types;
using System.Collections.Generic;

namespace Attempt19.CodeGeneration {
    public class TypeGenerator : ITypeVisitor<string> {
        private readonly ICScope scope;
        private readonly ICodeWriter header1Writer;
        private readonly ICodeWriter header2Writer;

        private bool arrayGenerated = false;

        private readonly Dictionary<LanguageType, string> generatedTypes
            = new Dictionary<LanguageType, string>();

        public TypeGenerator(ICScope scope, ICodeWriter header1, ICodeWriter header2) {
            this.scope = scope;
            this.header1Writer = header1;
            this.header2Writer = header2;
        }

        public string VisitArrayType(ArrayType type) {
            if (this.arrayGenerated) {
                return "$array";
            }

            this.header1Writer.Line("typedef struct $array {");
            this.header1Writer.Lines(CWriter.Indent("int64_t size;"));
            this.header1Writer.Lines(CWriter.Indent("uintptr_t data;"));
            this.header1Writer.Line("} $array;");
            this.header1Writer.EmptyLine();

            this.arrayGenerated = true;

            return "$array";
        }

        public string VisitBoolType(BoolType type) {
            return "uint16_t";
        }

        public string VisitIntType(IntType type) {
            return "int64_t";
        }
        public string VisitVariableType(VariableType type) {
            return "uintptr_t";
        }

        public string VisitVoidType(VoidType type) {
            return "uint16_t";
        }
    }
}