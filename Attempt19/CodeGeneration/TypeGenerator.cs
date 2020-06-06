using Attempt19.Types;
using System.Collections.Generic;

namespace Attempt19.CodeGeneration {
    public class TypeGenerator : ITypeVisitor<string> {
        private readonly ICodeWriter header1Writer;
        private readonly ICodeWriter header2Writer;

        private int arrayCounter = 0;

        private readonly Dictionary<LanguageType, string> generatedTypes
            = new Dictionary<LanguageType, string>();

        public TypeGenerator(ICodeWriter header1, ICodeWriter header2) {
            this.header1Writer = header1;
            this.header2Writer = header2;
        }

        public string VisitArrayType(ArrayType type) {
            if (this.generatedTypes.TryGetValue(type, out var name)) {
                return name;
            }

            name = "$array" + this.arrayCounter++;
            string elemName = type.ElementType.Accept(this);

            this.header1Writer.Line($"typedef struct {name} {{");
            this.header1Writer.Lines(CWriter.Indent("int64_t size;"));
            this.header1Writer.Lines(CWriter.Indent($"{elemName}* data;"));
            this.header1Writer.Line($"}} {name};");
            this.header1Writer.EmptyLine();

            generatedTypes[type] = name;

            return name;
        }

        public string VisitBoolType(BoolType type) {
            return "char";
        }

        public string VisitIntType(IntType type) {
            return "int64_t";
        }
        public string VisitVariableType(VariableType type) {
            return type.InnerType.Accept(this) + "*";
        }

        public string VisitVoidType(VoidType type) {
            return "char";
        }
    }
}