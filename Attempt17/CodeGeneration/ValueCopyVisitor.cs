using Attempt17.TypeChecking;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt17.CodeGeneration {
    public class ValueCopyVisitor : ITypeVisitor<CBlock> {
        private readonly string value;
        private readonly ICodeGenerator gen;
        private readonly ICScope scope;
        private static int arrayCopyTemp = 0;
        private static int structCopyTemp = 0;

        public ValueCopyVisitor(string value, ICodeGenerator gen, ICScope scope) {
            this.value = value;
            this.gen = gen;
            this.scope = scope;
        }

        public CBlock VisitArrayType(ArrayType type) {
            var tempName = "$array_copy_" + arrayCopyTemp++;
            var tempType = this.gen.Generate(type);
            var writer = new CWriter();

            writer.Line("// Array copy");
            writer.VariableInit(tempType, tempName, this.value);
            writer.Line($"{tempName}.data &= ~1;");
            writer.EmptyLine();

            return writer.ToBlock(tempName);
        }

        public CBlock VisitBoolType(BoolType type) {
            return new CBlock(this.value);
        }

        public CBlock VisitIntType(IntType type) {
            return new CBlock(this.value);
        }

        public CBlock VisitNamedType(NamedType type) {
            if (!this.scope.FindTypeInfo(type.Path).TryGetValue(out var info)) {
                throw new Exception("This is no supposed to happen");
            }

            return info.Match(
                varInfo => throw new InvalidOperationException(),
                funcInfo => new CBlock(this.value),
                structInfo => {
                    var destructorGen = new TypeDestructorGenerator(this.gen.Header3Writer, this.gen, this.scope);

                    // If all of the struct's members are unconditionally copiable,
                    // let C handle the copy implicitly
                    if (structInfo.Signature.Members.All(x => !x.Type.Accept(destructorGen).Any())) {
                        return new CBlock(this.value);
                    }

                    var tempName = "$struct_copy_" + structCopyTemp++;
                    var tempType = this.gen.Generate(type);
                    var writer = new CWriter();

                    // Copy all of the struct's members
                    var copies = structInfo
                        .Signature
                        .Members
                        .Select(x => {
                            var visitor = new ValueCopyVisitor($"{this.value}.{x.Name}", this.gen, this.scope);
                            var copy = x.Type.Accept(visitor);

                            return new {
                                MemberName = x.Name,
                                CopyValue = copy
                            };
                        })
                        .ToArray();

                    // Write out all of the copying code
                    foreach (var copy in copies) {
                        writer.Lines(copy.CopyValue.SourceLines);
                    }

                    // Write out the new struct
                    writer.Line("// Struct copy");
                    writer.VariableInit(tempType, tempName);

                    foreach (var mem in copies) {
                        writer.VariableAssignment($"{tempName}.{mem.MemberName}", mem.CopyValue.Value);
                    }

                    writer.EmptyLine();

                    return writer.ToBlock(tempName);
                });
        }

        public CBlock VisitVariableType(VariableType type) {
            var varTypeName = this.gen.Generate(type);

            return new CBlock(CWriter.MaskPointer(this.value));
        }

        public CBlock VisitVoidType(VoidType type) {
            return new CBlock(this.value);
        }
    }
}