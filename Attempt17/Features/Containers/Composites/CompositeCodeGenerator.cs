using System;
using System.Linq;
using Attempt17.CodeGeneration;
using Attempt17.Features.Variables;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Composites {
    public class CompositeCodeGenerator {
        private static int newCounter = 0;

        public CBlock GenerateCompositeDeclaration(CompositeDeclarationSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
            string name = syntax.CompositeInfo.Path.ToCName();

            if (syntax.CompositeInfo.Signature.Members.Any()) {
                // Generate forward declaration
                gen.Header1Writer.Line($"typedef struct {name} {name};");
                gen.Header1Writer.EmptyLine();

                // Generate struct definition
                gen.Header2Writer.Line($"struct {name} {{");

                foreach (var mem in syntax.CompositeInfo.Signature.Members) {
                    var memType = gen.Generate(mem.Type);

                    gen.Header2Writer.Lines(CWriter.Indent($"{memType} {mem.Name};"));
                }

                gen.Header2Writer.Line($"}};");
                gen.Header2Writer.EmptyLine();
            }
            else {
                // Generate forward declaration
                gen.Header1Writer.Line($"typedef uint16_t {name};");
                gen.Header1Writer.EmptyLine();
            }

            return new CBlock("0");
        }

        public CBlock GenerateNewCompositeSyntax(NewCompositeSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
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

            if (syntax.CompositeInfo.Kind == CompositeKind.Class) {
                var structName = syntax.CompositeInfo.Path.ToCName();

                // Write the temp variable
                writer.Line("// New class");
                writer.VariableInit(structType, tempName, $"(uintptr_t)malloc(sizeof({structName}))");

                foreach (var inst in insts) {
                    writer.VariableAssignment($"(({structName}*){tempName})->{inst.MemberName}", inst.Code.Value);
                }

                writer.Line($"{tempName} |= 1;");
            }
            else {
                // Write the temp variable
                writer.Line("// New struct");
                writer.VariableInit(structType, tempName);

                foreach (var inst in insts) {
                    writer.VariableAssignment($"{tempName}.{inst.MemberName}", inst.Code.Value);
                }
            }

            writer.EmptyLine();

            return writer.ToBlock(tempName);
        }

        public CBlock GenerateCompositeMemberAccess(CompositeMemberAccessSyntax syntax, ICScope scope, ICodeGenerator gen) {
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

            if (syntax.CompositeInfo.Kind == CompositeKind.Struct) {
                return writer.ToBlock($"({target.Value}.{syntax.MemberName})");
            }
            else {
                var structName = syntax.CompositeInfo.Path.ToCName();

                return writer.ToBlock($"((({structName}*)({target.Value} & ~1))->{syntax.MemberName})");
            }
        }
    }
}