using Attempt17.CodeGeneration;
using Attempt17.Features.Variables;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt17.Features.Arrays {
    public class ArraysCodeGenerator {
        private int arrayLiteralCounter = 0;

        public CBlock GenerateArrayRangeLiteral(ArrayRangeLiteralSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
            var count = gen.Generate(syntax.ElementCount, scope);
            var arrayType = gen.Generate(syntax.Tag.ReturnType);
            var tempName = "$array_init_" + this.arrayLiteralCounter++;
            var elemType = gen.Generate(syntax.ElementType);
            var writer = new CWriter();

            writer.Lines(count.SourceLines);
            writer.Line("// Array initialization");
            writer.VariableInit(arrayType, tempName);
            writer.Line($"{tempName}.size = {count.Value};");
            writer.Line($"{tempName}.data = 1 | (uintptr_t)calloc({count.Value}, sizeof({elemType}));");
            writer.EmptyLine();

            return writer.ToBlock(tempName);
        }

        public CBlock GenerateArrayIndex(ArrayIndexSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
            var target = gen.Generate(syntax.Target, scope);
            var index = gen.Generate(syntax.Index, scope);
            var typeName = gen.Generate(syntax.Tag.ReturnType);
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
            writer.Lines(index.SourceLines);

            return writer.ToBlock($"(({typeName}*)({target.Value}.data & ~1))[{index.Value}]");
        }

        public CBlock GenerateArrayStore(ArrayStoreSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
            var target = gen.Generate(syntax.Target, scope);
            var index = gen.Generate(syntax.Index, scope);
            var value = gen.Generate(syntax.Value, scope);
            var typeName = gen.Generate(syntax.Value.Tag.ReturnType);
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
            writer.Lines(index.SourceLines);
            writer.Lines(value.SourceLines);
            writer.Line("// Array store");
            writer.Line($"(({typeName}*)({target.Value}.data & ~1))[{index.Value}] = {value.Value};");
            writer.EmptyLine();

            return writer.ToBlock("0");
        }

        public CBlock GenerateArrayLiteral(ArrayLiteralSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
            var arrayType = (ArrayType)syntax.Tag.ReturnType;
            var carrayType = gen.Generate(syntax.Tag.ReturnType);
            var tempName = "$array_init_" + this.arrayLiteralCounter++;
            var elemType = gen.Generate(arrayType.ElementType);
            var elems = syntax.Elements.Select(x => gen.Generate(x, scope)).ToArray();
            var writer = new CWriter();

            foreach (var elem in elems) {
                writer.Lines(elem.SourceLines);
            }

            if (syntax.Elements.Any()) {
                writer.Line("// Array initialization");
                writer.VariableInit(carrayType, tempName);
                writer.Line($"{tempName}.size = {syntax.Elements.Count}LL;");
                writer.Line($"{tempName}.data = (uintptr_t)malloc({syntax.Elements.Count}LL, sizeof({elemType}));");

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

        public CBlock GenerateSizeAccess(ArraySizeAccessSyntax syntax, ICScope scope, ICodeGenerator gen) {
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

            return writer.ToBlock($"({target.Value}.size)");
        }
    }
}