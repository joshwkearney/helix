using System;
using Attempt17.CodeGeneration;

namespace Attempt17.Features.Structs {
    public class StructsCodeGenerator {
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
    }
}