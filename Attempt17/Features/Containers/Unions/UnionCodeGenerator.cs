using System;
using System.Linq;
using Attempt17.CodeGeneration;
using Attempt17.Features.Functions;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Containers.Unions {
    public class UnionCodeGenerator : IUnionVisitor<CBlock, TypeCheckTag, CodeGenerationContext> {
        private static int newCounter = 0;

        public CBlock VisitNewUnion(NewUnionSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var writer = new CWriter();
            var tempName = "$union_new_" + newCounter++;
            var structType = context.Generator.Generate(syntax.Tag.ReturnType);

            var index = syntax
                .UnionInfo
                .Signature
                .Members
                .Select(x => x.Name)
                .ToList()
                .IndexOf(syntax.Instantiation.MemberName);

            // Generate all the member instantiations
            var inst = syntax.Instantiation.Value.Accept(visitor, context);

            // Write out the instantiation
            writer.Lines(inst.SourceLines);
            
            // Write the temp variable
            writer.Line("// New union");
            writer.VariableInit(structType, tempName);

            writer.VariableAssignment(
                $"{tempName}.data.{syntax.Instantiation.MemberName}",
                inst.Value);

            writer.VariableAssignment($"{tempName}.tag", index.ToString());
            writer.EmptyLine();

            return writer.ToBlock(tempName);
        }

        public CBlock VisitParseUnionDeclaration(ParseUnionDeclarationSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            throw new InvalidOperationException();
        }

        public CBlock VisitUnionDeclaration(UnionDeclarationSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var writer = new CWriter();

            // Generate all the union methods
            foreach (var method in syntax.Methods) {
                this.GenerateUnionMethod(method, syntax, writer, visitor, context);
            }

            return writer.ToBlock("0");
        }

        private void GenerateUnionMethod(FunctionSignature method,
            UnionDeclarationSyntax<TypeCheckTag> syntax, CWriter writer,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var actualMethod = new FunctionSignature(
                method.Name,
                method.ReturnType,
                method.Parameters.Insert(0, new Parameter("this", syntax.UnionInfo.Type)));

            var path = syntax.UnionInfo.Path.Append(method.Name);
            var sig = FunctionsCodeGenerator.GenerateSignature(actualMethod, path, context);

            // Write the forward declaration
            context.Generator.Header2Writer.Line(sig + ";").EmptyLine();

            // Write out the function body
            writer
                .Line(sig + " {")
                .Lines(CWriter.Indent(1, "switch (this.tag) {"));

            foreach (var mem in syntax.UnionInfo.Signature.Members) {
                var key = (mem, method);

                if (syntax.ParameterMethods.TryGetValue(key, out var target)) {
                    var index = syntax.UnionInfo.Signature.Members.IndexOf(mem);
                    var args = method.Parameters.Select(x => x.Name).Prepend($"this.data.{mem.Name}");
                    var invoke = target.Path.ToCName()
                        + "(" + string.Join(", ", args) + ")";

                    writer.Lines(CWriter.Indent(2, $"case {index}:"));
                    writer.Lines(CWriter.Indent(3, $"return {invoke};"));
                }
            }

            writer
                .Lines(CWriter.Indent(1, "}"))
                .Line("}")
                .EmptyLine();
        }
    }
}
