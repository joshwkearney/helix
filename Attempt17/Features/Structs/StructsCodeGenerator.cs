using System;
using System.Linq;
using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Structs {
    public class StructsCodeGenerator {
        private static int newCounter = 0;

        public CBlock GenerateStructDeclaration(StructDeclarationSyntaxTree syntax, ICScope scope, ICodeGenerator gen) {
            // Generate forward declaration
            gen.Header1Writer.Line($"typedef struct {syntax.Info.Signature.Name} {syntax.Info.Signature.Name};");
            gen.Header1Writer.EmptyLine();

            // Generate struct definition
            gen.Header2Writer.Line($"struct {syntax.Info.Signature.Name} {{");

            foreach (var mem in syntax.Info.Signature.Members) {
                var memType = gen.Generate(mem.Type);

                gen.Header2Writer.Lines(CWriter.Indent($"{memType} {mem.Name};"));
            }

            gen.Header2Writer.Line($"}}");
            gen.Header2Writer.EmptyLine();

            return new CBlock("0");
        }

        public CBlock GenerateNewSyntax(NewSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
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
    }
}