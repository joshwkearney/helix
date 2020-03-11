using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.CodeGeneration {
    public class TypeDestructorGenerator : ITypeVisitor<IOption<string>> {
        private readonly ICodeWriter headerWriter;
        private readonly ICodeGenerator gen;
        private readonly Dictionary<LanguageType, string> generatedTypes = new Dictionary<LanguageType, string>();

        public TypeDestructorGenerator(ICodeWriter headerWriter, ICodeGenerator gen) {
            this.headerWriter = headerWriter;
            this.gen = gen;
        }

        public IOption<string> VisitArrayType(ArrayType type) {
            if (this.generatedTypes.TryGetValue(type, out string value)) {
                return Option.Some(value);
            }

            var destructorName = "$destructor_" + type.ToFriendlyString();
            var arrayTypeName = this.gen.Generate(type);
            var dataTypeName = this.gen.Generate(type.ElementType);
            var hasInnerDestructor = type.ElementType.Accept(this).TryGetValue(out string innerDestructor);

            this.headerWriter.Line($"inline void {destructorName}({arrayTypeName} obj) {{");
            this.headerWriter.Lines(CWriter.Indent($"if ((obj.data & 1) == 1) {{"));

            if (hasInnerDestructor) {
                var intType = this.gen.Generate(IntType.Instance);
                var innerType = this.gen.Generate(type.ElementType);
           
                this.headerWriter.Lines(
                    CWriter.Indent(2, $"for ({intType} i = 0; i < obj.size; i++) {{"));
                this.headerWriter.Lines(
                    CWriter.Indent(3, $"{innerType} val = (({innerType}*)(obj.data & ~1))[i];"));
                this.headerWriter.Lines(
                    CWriter.Indent(3, $"{innerDestructor}(val);"));
                this.headerWriter.Lines(CWriter.Indent(2, "}"));
                this.headerWriter.EmptyLine();
            }

            this.headerWriter
                .Lines(CWriter.Indent(2, $"free(({dataTypeName}*)(obj.data & ~1));"))
                .Lines(CWriter.Indent("}"))
                .Line("}")
                .EmptyLine();

            this.generatedTypes[type] = destructorName;

            return Option.Some(destructorName);
        }

        public IOption<string> VisitBoolType(BoolType type) => Option.None<string>();

        public IOption<string> VisitIntType(IntType type) => Option.None<string>();

        public IOption<string> VisitNamedType(NamedType type) => Option.None<string>();

        public IOption<string> VisitVariableType(VariableType type) {
            if (this.generatedTypes.TryGetValue(type, out string value)) {
                return Option.Some(value);
            }

            var destructorName = "$destructor_" + type.ToFriendlyString();
            var varTypeName = this.gen.Generate(type);
            var innerType = this.gen.Generate(type.InnerType);

            this.headerWriter.Line($"inline void {destructorName}({varTypeName} obj) {{");
            this.headerWriter.Lines(CWriter.Indent($"if ((obj & 1) == 1) {{"));

            if (type.InnerType.Accept(this).TryGetValue(out string innerDestructor)) {
                this.headerWriter.Lines(
                    CWriter.Indent(2, $"{innerDestructor}(*({innerType}*)(obj & ~1));"));
            }

            this.headerWriter
                .Lines(CWriter.Indent(2, $"free(({innerType}*)(obj & ~1));"))
                .Lines(CWriter.Indent("}"))
                .Line("}")
                .EmptyLine();

            this.generatedTypes[type] = destructorName;

            return Option.Some(destructorName);
        }

        public IOption<string> VisitVoidType(VoidType type) => Option.None<string>();
    }
}