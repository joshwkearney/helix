using System;
using System.Linq;
using Attempt17.CodeGeneration;
using Attempt17.Features.Variables;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Structs {
    public class StructsCodeGenerator {
        private static int newCounter = 0;

        public CBlock GenerateStructDeclaration(StructDeclarationSyntax<TypeCheckTag>  syntax, ICScope scope, ICodeGenerator gen) {
            string name = syntax.StructInfo.Path.ToCName();

            // Generate forward declaration
            gen.Header1Writer.Line($"typedef struct {name} {name};");
            gen.Header1Writer.EmptyLine();

            // Generate struct definition
            gen.Header2Writer.Line($"struct {name} {{");

            foreach (var mem in syntax.StructInfo.Signature.Members) {
                var memType = gen.Generate(mem.Type);

                gen.Header2Writer.Lines(CWriter.Indent($"{memType} {mem.Name};"));
            }

            gen.Header2Writer.Line($"}}");
            gen.Header2Writer.EmptyLine();

            return new CBlock("0");
        }

        public CBlock GenerateNewStructSyntax(NewStructSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
            var writer = new CWriter();
            var tempName = "$struct_new_" + newCounter++;
            var structType = gen.Generate(syntax.Tag.ReturnType);

            // Generate all the member instantiations
            var insts = syntax.Instantiations
                .Select(x => {
                    var code = gen.Generate(x.Value, scope);

                    return new {
                        Code = code,
                        x.MemberName
                    };
                })
                .ToArray();

            // Write out all of the instantiations
            foreach (var inst in insts) {
                writer.Lines(inst.Code.SourceLines);
            }

            // Write the temp variable
            writer.Line("// New struct");
            writer.VariableInit(structType, tempName);

            foreach (var inst in insts) {
                writer.VariableAssignment($"{tempName}.{inst.MemberName}", inst.Code.Value);
            }

            writer.EmptyLine();

            return writer.ToBlock(tempName);
        }

        public CBlock GenerateStructMemberAccess(StructMemberAccessSyntax syntax, ICScope scope, ICodeGenerator gen) {
            var target = gen.Generate(syntax.Target, scope);
            var writer = new CWriter();

            // Optimization: If the target is a variable access, don't force a copy
            // of the entire struct
            if (syntax.Target is VariableAccessSyntax access) {
                if (access.Kind == VariableAccessKind.ValueAccess) {
                    // This would have produced another variable to clean up, so remove
                    // that from the scope
                    scope.SetVariableDestructed(target.Value);

                    target = new CBlock(access.VariableInfo.Path.Segments.Last());
                }
            }

            writer.Lines(target.SourceLines);

            return writer.ToBlock($"({target.Value}.{syntax.MemberName})");
        }
    }
}