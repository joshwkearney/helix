using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Attempt17.CodeGeneration {
    public class TypeDestructorGenerator : ITypeVisitor<IOption<string>> {
        private readonly ICodeWriter headerWriter;
        private readonly ICodeGenerator gen;
        private readonly ICScope scope;
        private readonly Dictionary<LanguageType, IOption<string>> generatedTypes = new Dictionary<LanguageType, IOption<string>>();

        public TypeDestructorGenerator(ICodeWriter headerWriter, ICodeGenerator gen, ICScope scope) {
            this.headerWriter = headerWriter;
            this.gen = gen;
            this.scope = scope;
        }

        public IOption<string> VisitArrayType(ArrayType type) {
            if (this.generatedTypes.TryGetValue(type, out var value)) {
                return value;
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

            this.generatedTypes[type] = Option.Some(destructorName);

            return Option.Some(destructorName);
        }

        public IOption<string> VisitBoolType(BoolType type) => Option.None<string>();

        public IOption<string> VisitIntType(IntType type) => Option.None<string>();

        public IOption<string> VisitNamedType(NamedType type) {
            if (this.generatedTypes.TryGetValue(type, out var value)) {
                return value;
            }

            if (!this.scope.FindTypeInfo(type.Path).TryGetValue(out var info)) {
                throw new Exception("This is not supposed to happen");
            }

            return info.Match(
                varInfo => throw new InvalidOperationException(),
                funcInfo => Option.None<string>(),
                structInfo => {
                    var memberDestructors = structInfo
                        .Signature
                        .Members
                        .SelectMany(x => x.Type.Accept(this).AsEnumerable().Select(y => new {
                            x.Name,
                            Destructor = y
                        }))
                        .ToArray();

                    if (!memberDestructors.Any()) {
                        this.generatedTypes[type] = Option.None<string>();
                        return Option.None<string>();
                    }

                    var destructorName = "$destructor_" + type.ToFriendlyString();
                    var structTypeName = this.gen.Generate(type);

                    this.headerWriter.Line($"inline void {destructorName}({structTypeName} obj) {{");

                    foreach (var destructor in memberDestructors) {
                        this.headerWriter.Lines(CWriter.Indent($"{destructor.Destructor}(obj.{destructor.Name});"));
                    }

                    this.headerWriter.Line("}");
                    this.headerWriter.EmptyLine();

                    this.generatedTypes[type] = Option.Some(destructorName);

                    return Option.Some(destructorName);
                });
        }

        public IOption<string> VisitVariableType(VariableType type) {
            if (this.generatedTypes.TryGetValue(type, out var value)) {
                return value;
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

            this.generatedTypes[type] = Option.Some(destructorName);

            return Option.Some(destructorName);
        }

        public IOption<string> VisitVoidType(VoidType type) => Option.None<string>();
    }
}