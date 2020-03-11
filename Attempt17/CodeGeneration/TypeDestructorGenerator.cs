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
                compositeInfo => {
                    var memberDestructors = compositeInfo
                        .Signature
                        .Members
                        .SelectMany(x => x.Type.Accept(this).AsEnumerable().Select(y => new {
                            MemberName = x.Name,
                            Destructor = y
                        }))
                        .ToArray();

                    // If we're a struct and none of the members have destructors, we don't need a destructor
                    if (!memberDestructors.Any() && compositeInfo.Kind == TypeChecking.CompositeKind.Struct) {
                        this.generatedTypes[type] = Option.None<string>();
                        return Option.None<string>();
                    }

                    var destructorName = "$destructor_" + type.ToFriendlyString();
                    var structTypeName = this.gen.Generate(type);
                    var structName = compositeInfo.Path.ToCName();

                    // Write the function signature
                    this.headerWriter.Line($"inline void {destructorName}({structTypeName} obj) {{");

                    // If we're a class, make sure the destructor bit is set to 1
                    if (compositeInfo.Kind == TypeChecking.CompositeKind.Class) {
                        this.headerWriter.Lines(CWriter.Indent($"if ((obj & 1) == 0) {{"));
                        this.headerWriter.Lines(CWriter.Indent(2, "return;"));
                        this.headerWriter.Lines(CWriter.Indent("}"));
                        this.headerWriter.Lines(CWriter.Indent("else {"));
                        this.headerWriter.Lines(CWriter.Indent(2, "obj &= ~1;"));
                        this.headerWriter.Lines(CWriter.Indent("}"));
                        this.headerWriter.EmptyLine();
                    }

                    // Write the member destructors
                    foreach (var destructor in memberDestructors) {
                        if (compositeInfo.Kind == TypeChecking.CompositeKind.Struct) {
                            this.headerWriter.Lines(CWriter.Indent($"{destructor.Destructor}(obj.{destructor.MemberName});"));
                        }
                        else {
                            this.headerWriter.Lines(CWriter.Indent($"{destructor.Destructor}((({structName}*)obj)->{destructor.MemberName});"));
                        }
                    }

                    if (memberDestructors.Any()) {
                        this.headerWriter.EmptyLine();
                    }

                    // If we're a class, free the pointer also
                    if (compositeInfo.Kind == TypeChecking.CompositeKind.Class) {
                        this.headerWriter.Lines(CWriter.Indent($"free(({structName}*)obj);"));
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