using System;
using System.Linq;
using Attempt17.CodeGeneration;
using Attempt17.Features.Variables;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Containers.Arrays {
    public class ArraysCodeGenerator : IArraysVisitor<CBlock, TypeCheckTag, CodeGenerationContext> {
        private int arrayLiteralCounter = 0;

        public CBlock VisitArrayRangeLiteral(ArrayRangeLiteralSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var count = syntax.ElementCount.Accept(visitor, context);
            var arrayType = context.Generator.Generate(syntax.Tag.ReturnType);
            var tempName = "$array_init_" + this.arrayLiteralCounter++;
            var elemType = context.Generator.Generate(syntax.ElementType);
            var writer = new CWriter();

            writer.Lines(count.SourceLines);
            writer.Line("// Array initialization");
            writer.VariableInit(arrayType, tempName);
            writer.Line($"{tempName}.size = {count.Value};");
            writer.Line($"{tempName}.data = 1 | (uintptr_t)calloc({count.Value}, sizeof({elemType}));");
            writer.EmptyLine();

            return writer.ToBlock(tempName);
        }

        public CBlock VisitIndex(ArrayIndexSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var target = syntax.Target.Accept(visitor, context);
            var index = syntax.Index.Accept(visitor, context);
            var typeName = context.Generator.Generate(syntax.Tag.ReturnType);
            var writer = new CWriter();

            // Optimization: If the target is a variable access, don't force a copy
            // of the entire struct
            if (syntax.Target is VariableAccessSyntax<TypeCheckTag> access) {
                if (access.Kind == VariableAccessKind.ValueAccess) {
                    // This would have produced another variable to clean up, so remove
                    // that from the scope
                    context.Scope.SetVariableDestructed(target.Value);

                    target = new CBlock(access.VariableInfo.Path.Segments.Last());
                }
            }

            writer.Lines(target.SourceLines);
            writer.Lines(index.SourceLines);

            return writer.ToBlock($"(({typeName}*)({target.Value}.data & ~1))[{index.Value}]");
        }

        public CBlock VisitLiteral(ArrayLiteralSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var arrayType = (ArrayType)syntax.Tag.ReturnType;
            var carrayType = context.Generator.Generate(syntax.Tag.ReturnType);
            var tempName = "$array_init_" + this.arrayLiteralCounter++;
            var elemType = context.Generator.Generate(arrayType.ElementType);
            var elems = syntax.Elements.Select(x => x.Accept(visitor, context)).ToArray();
            var writer = new CWriter();

            foreach (var elem in elems) {
                writer.Lines(elem.SourceLines);
            }

            if (syntax.Elements.Any()) {
                writer.Line("// Array initialization");
                writer.VariableInit(carrayType, tempName);
                writer.Line($"{tempName}.size = {syntax.Elements.Count}LL;");
                writer.Line($"{tempName}.data = (uintptr_t)malloc({syntax.Elements.Count}LL * sizeof({elemType}));");

                for (int i = 0; i < syntax.Elements.Count; i++) {
                    writer.Line($"(({elemType}*)({tempName}.data))[{i}] = {elems[i].Value};");
                }

                writer.Line($"{tempName}.data |= 1;");
            }
            else {
                writer.Line("// Array initialization");
                writer.VariableInit(carrayType, tempName);
                writer.Line($"{tempName}.size = 0;");
                writer.Line($"{tempName}.data = 0;");
            }

            writer.EmptyLine();

            return writer.ToBlock(tempName);
        }

        public CBlock VisitSizeAccess(ArraySizeAccessSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var target = syntax.Target.Accept(visitor, context);
            var writer = new CWriter();

            // Optimization: If the target is a variable access, don't force a copy
            // of the entire struct
            if (syntax.Target is VariableAccessSyntax<TypeCheckTag> access) {
                if (access.Kind == VariableAccessKind.ValueAccess) {
                    // This would have produced another variable to clean up, so remove
                    // that from the scope
                    context.Scope.SetVariableDestructed(target.Value);

                    target = new CBlock(access.VariableInfo.Path.Segments.Last());
                }
            }

            writer.Lines(target.SourceLines);

            return writer.ToBlock($"({target.Value}.size)");
        }

        public CBlock VisitStore(ArrayStoreSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var target = syntax.Target.Accept(visitor, context);
            var index = syntax.Index.Accept(visitor, context);
            var value = syntax.Value.Accept(visitor, context);
            var typeName = context.Generator.Generate(syntax.Value.Tag.ReturnType);
            var writer = new CWriter();

            // Optimization: If the target is a variable access, don't force a copy
            // of the entire struct
            if (syntax.Target is VariableAccessSyntax<TypeCheckTag> access) {
                if (access.Kind == VariableAccessKind.ValueAccess) {
                    // This would have produced another variable to clean up, so remove
                    // that from the scope
                    context.Scope.SetVariableDestructed(target.Value);

                    target = new CBlock(access.VariableInfo.Path.Segments.Last());
                }
            }

            writer.Lines(target.SourceLines);
            writer.Lines(index.SourceLines);
            writer.Lines(value.SourceLines);
            writer.Line("// Array store");
            writer.Line($"(({typeName}*)({target.Value}.data & ~1))[{index.Value}] = {value.Value};");
            writer.EmptyLine();

            return writer.ToBlock("0");
        }
    }
}
