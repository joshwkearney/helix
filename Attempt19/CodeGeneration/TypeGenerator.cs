using Attempt18.TypeChecking;
using Attempt18.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt18.CodeGeneration {
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

        public string VisitNamedType(NamedType type) {
            if (this.generatedTypes.TryGetValue(type, out var name)) {
                return name;
            }

            if (!this.scope.FindTypeInfo(type.Path).TryGetValue(out var info)) {
                throw new Exception("This isn't supposed to happen");
            }

            return info.Accept(new IdentifierTargetVisitor<string>() {
                HandleFunction = _ => "uint16_t",
                HandleComposite = structInfo => {
                    if (structInfo.Kind == CompositeKind.Union) {
                        this.GenerateUnion(structInfo);
                    }
                    else if (structInfo.Kind == CompositeKind.Struct) {
                        this.GenerateComposite(structInfo);
                    }
                    else if (structInfo.Kind == CompositeKind.Class) {
                        this.GenerateComposite(structInfo);
                    }
                    else {
                        throw new Exception("This is not supposed to happen.");
                    }

                    if (structInfo.Kind == CompositeKind.Class) {
                        return "uintptr_t";
                    }
                    else {
                        return structInfo.Path.ToCName();
                    }
                }
            });
        }

        private void GenerateUnion(CompositeInfo unionInfo) {
            var name = unionInfo.Path.ToCName();
            this.generatedTypes[unionInfo.Type] = name;

            if (unionInfo.Signature.Members.Any()) {
                // Generate forward declaration
                this.header1Writer.Line($"typedef struct {name} {name};");
                this.header1Writer.EmptyLine();

                // Generate all of the member types before we start writing
                foreach (var mem in unionInfo.Signature.Members) {
                    mem.Type.Accept(this);
                }

                // Generate struct definition
                this.header2Writer
                    .Line($"struct {name} {{")
                    .Lines(CWriter.Indent(1, "union {"));

                // Generate members
                foreach (var mem in unionInfo.Signature.Members) {
                    var memType = mem.Type.Accept(this);

                    this.header2Writer
                        .Lines(CWriter.Indent(2, $"{memType} {mem.Name};"));
                }

                // Close
                this.header2Writer
                    .Lines(CWriter.Indent(1, "} data;"))
                    .EmptyLine()
                    .Lines(CWriter.Indent(1, "uint16_t tag;"))
                    .Line("};")
                    .EmptyLine();
            }
            else {
                // Generate forward declaration
                this.header1Writer.Line($"typedef uint16_t {name};");
                this.header1Writer.EmptyLine();
            }
        }

        private void GenerateComposite(CompositeInfo compositeInfo) {
            string name = compositeInfo.Path.ToCName();

            if (compositeInfo.Kind == CompositeKind.Struct) {
                this.generatedTypes[compositeInfo.Type] = name;
            }
            else {
                this.generatedTypes[compositeInfo.Type] = "uintptr_t";
            }

            if (compositeInfo.Signature.Members.Any()) {
                // Generate forward declaration
                this.header1Writer.Line($"typedef struct {name} {name};");
                this.header1Writer.EmptyLine();

                // Generate all of the member types before we start writing
                foreach (var mem in compositeInfo.Signature.Members) {
                    mem.Type.Accept(this);
                }

                // Generate struct definition
                this.header2Writer.Line($"struct {name} {{");

                foreach (var mem in compositeInfo.Signature.Members) {
                    var memType = mem.Type.Accept(this);

                    this.header2Writer.Lines(CWriter.Indent($"{memType} {mem.Name};"));
                }

                this.header2Writer.Line($"}};");
                this.header2Writer.EmptyLine();
            }
            else {
                if (compositeInfo.Kind == CompositeKind.Struct) {
                    // Generate forward declaration
                    this.header1Writer.Line($"typedef uint16_t {name};");
                    this.header1Writer.EmptyLine();
                }
                else {
                    // Generate forward declaration
                    this.header1Writer.Line($"typedef uintptr_t {name};");
                    this.header1Writer.EmptyLine();
                }
            }
        }

        public string VisitVariableType(VariableType type) {
            return "uintptr_t";
        }

        public string VisitVoidType(VoidType type) {
            return "uint16_t";
        }
    }
}