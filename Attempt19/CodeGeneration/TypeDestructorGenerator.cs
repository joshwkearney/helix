using Attempt18.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Attempt18.TypeChecking;

namespace Attempt18.CodeGeneration {
    public class TypeDestructorGenerator : ITypeVisitor<IOption<string>> {
        private readonly ICodeWriter headerWriter;
        private readonly ICodeGenerator gen;
        private readonly ICScope scope;
        private readonly Dictionary<LanguageType, IOption<string>> generatedTypes
            = new Dictionary<LanguageType, IOption<string>>();

        public TypeDestructorGenerator(ICodeWriter headerWriter, ICodeGenerator gen,
                                       ICScope scope) {
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

            var hasInnerDestructor = type.ElementType
                .Accept(this)
                .TryGetValue(out string innerDestructor);

            // Write forward declaration
            this.gen
                .Header2Writer
                .Line($"inline void {destructorName}({arrayTypeName} obj);")
                .EmptyLine();

            this.headerWriter.Line($"void {destructorName}({arrayTypeName} obj) {{");
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

            return info.Accept(new IdentifierTargetVisitor<IOption<string>>() {
                HandleFunction = _ => Option.None<string>(),
                HandleComposite = compositeInfo => {
                    if (!compositeInfo.Signature.Members.Any()) {
                        return Option.None<string>();
                    }

                    if (compositeInfo.Kind == CompositeKind.Struct) {
                        return this.DestructStruct(type, compositeInfo);
                    }
                    else if (compositeInfo.Kind == CompositeKind.Class) {
                        return this.DestructClass(type, compositeInfo);
                    }
                    else if (compositeInfo.Kind == CompositeKind.Union) {
                        return this.DestructUnion(type, compositeInfo);
                    }
                    else {
                        throw new Exception();
                    }
                }
            });
        }

        private IOption<string> DestructUnion(NamedType type, CompositeInfo compositeInfo) {
            var destructorName = "$destructor_" + type.ToFriendlyString();
            this.generatedTypes[type] = Option.Some(destructorName);

            var memberDestructors = compositeInfo
                .Signature
                .Members
                .SelectMany((x, i) => x.Type.Accept(this).AsEnumerable().Select(y => new {
                    MemberName = x.Name,
                    Destructor = y,
                    Index = i
                }))
                .ToArray();

            // If none of the members have destructors, we don't need a destructor
            if (!memberDestructors.Any()) {
                this.generatedTypes[type] = Option.None<string>();
                return Option.None<string>();
            }

            var unionTypeName = this.gen.Generate(type);

            // Write out the forward declaration
            this.gen
                .Header2Writer
                .Line($"inline void {destructorName}({unionTypeName} obj);")
                .EmptyLine();

            // Write the function signature
            this.headerWriter.Line($"void {destructorName}({unionTypeName} obj) {{");
            this.headerWriter.Lines(CWriter.Indent("switch (obj.tag) {"));

            // Write the member destructors
            foreach (var destructor in memberDestructors) {
                this.headerWriter.Lines(CWriter.Indent(2, $"case {destructor.Index}: {{"));
                this.headerWriter.Lines(CWriter.Indent(3, $"{destructor.Destructor}(obj.data.{destructor.MemberName});"));
                this.headerWriter.Lines(CWriter.Indent(3, $"break;"));
                this.headerWriter.Lines(CWriter.Indent(2, $"}}"));
            }

            this.headerWriter.Lines(CWriter.Indent(1, $"}}"));
            this.headerWriter.Line("}");
            this.headerWriter.EmptyLine();

            return Option.Some(destructorName);
        }

        private IOption<string> DestructStruct(NamedType type, CompositeInfo compositeInfo) {
            var destructorName = "$destructor_" + type.ToFriendlyString();
            this.generatedTypes[type] = Option.Some(destructorName);

            var memberDestructors = compositeInfo
                .Signature
                .Members
                .SelectMany(x => x.Type.Accept(this).AsEnumerable().Select(y => new {
                    MemberName = x.Name,
                    Destructor = y
                }))
                .ToArray();

            // If none of the members have destructors, we don't need a destructor
            if (!memberDestructors.Any()) {
                this.generatedTypes[type] = Option.None<string>();
                return Option.None<string>();
            }

            var structTypeName = this.gen.Generate(type);

            // Write out the forward declaration
            this.gen
                .Header2Writer
                .Line($"inline void {destructorName}({structTypeName} obj);")
                .EmptyLine();

            // Write the function signature
            this.headerWriter.Line($"void {destructorName}({structTypeName} obj) {{");

            // Write the member destructors
            foreach (var destructor in memberDestructors) {
                this.headerWriter.Lines(CWriter.Indent($"{destructor.Destructor}(obj.{destructor.MemberName});"));
            }

            this.headerWriter.Line("}");
            this.headerWriter.EmptyLine();

            return Option.Some(destructorName);
        }

        private IOption<string> DestructClass(NamedType type, CompositeInfo compositeInfo) {
            var destructorName = "$destructor_" + type.ToFriendlyString();
            this.generatedTypes[type] = Option.Some(destructorName);

            var memberDestructors = compositeInfo
                .Signature
                .Members
                .SelectMany(x => x.Type.Accept(this).AsEnumerable().Select(y => new {
                    MemberName = x.Name,
                    Destructor = y
                }))
                .ToArray();

            var structTypeName = this.gen.Generate(type);
            var structName = compositeInfo.Path.ToCName();

            // Write out the forward declaration
            this.gen
                .Header2Writer
                .Line($"inline void {destructorName}({structTypeName} obj);")
                .EmptyLine();

            // Write the function signature
            this.headerWriter.Line($"void {destructorName}({structTypeName} obj) {{");

            // Make sure the destructor bit is set to 1
            this.headerWriter.Lines(CWriter.Indent($"if ((obj & 1) == 1) {{"));

            // Write the member destructors
            foreach (var destructor in memberDestructors) {
                this.headerWriter.Lines(CWriter.Indent(2, $"{destructor.Destructor}((({structName}*)(obj & ~1))->{destructor.MemberName});"));
            }

            // Also free the pointer
            this.headerWriter.Lines(CWriter.Indent(2, $"free(({structName}*)(obj & ~1));"));
            this.headerWriter.Lines(CWriter.Indent("}"));
            this.headerWriter.Line("}");
            this.headerWriter.EmptyLine();

            return Option.Some(destructorName);
        }

        public IOption<string> VisitVariableType(VariableType type) {
            if (this.generatedTypes.TryGetValue(type, out var value)) {
                return value;
            }

            var destructorName = "$destructor_" + type.ToFriendlyString();
            this.generatedTypes[type] = Option.Some(destructorName);

            var varTypeName = this.gen.Generate(type);
            var innerType = this.gen.Generate(type.InnerType);

            // Write out the forward declaration
            this.gen
                .Header2Writer
                .Line($"inline void {destructorName}({varTypeName} obj);")
                .EmptyLine();

            this.headerWriter.Line($"void {destructorName}({varTypeName} obj) {{");
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


            return Option.Some(destructorName);
        }

        public IOption<string> VisitVoidType(VoidType type) => Option.None<string>();
    }
}