using System;
using System.Linq;
using Attempt17.CodeGeneration;
using Attempt17.Features.Variables;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Composites {
    public class CompositesCodeGenerator
        : ICompositesVisitor<CBlock, TypeCheckTag, CodeGenerationContext> {

        private static int newCounter = 0;
        private static int memberAcessCounter = 0;

        public CBlock VisitCompositeDeclaration(CompositeDeclarationSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {            

            return new CBlock("0");
        }

        public CBlock VisitCompositeMemberAccess(CompositeMemberAccessSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var target = syntax.Target.Accept(visitor, context);
            var writer = new CWriter();

            // Optimization: If the target is a variable access, don't force a copy of the entire
            // struct
            if (syntax.Target is VariableAccessSyntax<TypeCheckTag> access) {
                if (access.Kind == VariableAccessKind.ValueAccess) {
                    // This would have produced another variable to clean up, so remove that from
                    // the scope
                    context.Scope.SetVariableDestructed(target.Value);

                    target = new CBlock(access.VariableInfo.Path.Segments.Last());
                }
            }

            writer.Lines(target.SourceLines);

            if (syntax.CompositeInfo.Kind == CompositeKind.Struct) {
                return writer.ToBlock($"({target.Value}.{syntax.MemberName})");
            }
            else {
                var structName = syntax.CompositeInfo.Path.ToCName();
                var tempName = "$member_access_" + memberAcessCounter++;
                var tempType = context.Generator.Generate(syntax.Tag.ReturnType);

                writer.Line("// Class member access");
                writer.VariableInit(tempType, tempName,
                    $"((({structName}*)({target.Value} & ~1))->{syntax.MemberName})");
                writer.EmptyLine();

                return writer.ToBlock(tempName);
            }
        }

        public CBlock VisitNewComposite(NewCompositeSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var writer = new CWriter();
            var tempName = "$struct_new_" + newCounter++;
            var structType = context.Generator.Generate(syntax.Tag.ReturnType);

            // Generate all the member instantiations
            var insts = syntax.Instantiations
                .Select(x => {
                    var code = x.Value.Accept(visitor, context);

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

            if (insts.Any()) {
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
            }
            else {
                // Write the temp variable
                writer.Line("// New class");
                writer.VariableInit(structType, tempName, $"0");
            }

            writer.EmptyLine();

            return writer.ToBlock(tempName);
        }
    }
}
