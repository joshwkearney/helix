using System.Collections.Immutable;
using Attempt19.TypeChecking;
using Attempt19.CodeGeneration;
using Attempt19.Parsing;
using Attempt19.Types;
using System.Linq;
using System.Collections.Generic;
using Attempt19.Features.Containers.Arrays;
using System;

namespace Attempt19 {
    public static partial class SyntaxFactory {
        public static Syntax MakeArrayLiteral(IReadOnlyList<Syntax> elems, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new ArrayLiteralData() {
                    Elements = elems,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(ArrayLiteralTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Containers.Arrays {
    public class ArrayLiteralData : IParsedData, ITypeCheckedData, IFlownData {
        public IReadOnlyList<Syntax> Elements { get; set; }

        public IdentifierPath ContainingScope { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<VariableCapture> EscapingVariables { get; set; }
    }    

    public static class ArrayLiteralTransformations {
        private static int arrayLiteralCheckCounter = 0;
        private static int arrayLiteralCGCounter = 0;

        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope, NameCache names) {
            var literal = (ArrayLiteralData)data;

            // Delegate name declaration
            literal.Elements = literal.Elements
                .Select(x => x.DeclareNames(scope, names))
                .ToArray();

            // Set containing scope
            literal.ContainingScope = scope;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var literal = (ArrayLiteralData)data;

            // Delegate name resolution
            literal.Elements = literal.Elements
                .Select(x => x.ResolveNames(names))
                .ToArray();

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var literal = (ArrayLiteralData)data;

            // Delegate type declaration
            literal.Elements = literal.Elements
                .Select(x => x.DeclareTypes(types))
                .ToArray();

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var literal = (ArrayLiteralData)data;

            // Delegate type resolution
            literal.Elements = literal.Elements
                .Select(x => x.ResolveTypes(types, unifier))
                .ToArray();

            // Make sure we have at least one element
            if (!literal.Elements.Any()) {
                throw TypeCheckingErrors.ZeroLengthArrayLiteral(literal.Location);
            }

            var firstType = literal.Elements
                .First()
                .Data
                .AsTypeCheckedData()
                .GetValue()
                .ReturnType;

            // Make sure all the elements match the type of the first element
            foreach (var elem in literal.Elements.Skip(1)) {
                var elemData = elem.Data.AsTypeCheckedData().GetValue();

                if (firstType != elemData.ReturnType) {
                    throw TypeCheckingErrors.UnexpectedType(
                        elemData.Location, firstType, elemData.ReturnType);
                }
            }

            // Set return type
            literal.ReturnType = new ArrayType(firstType);

            // Set escaping variables
            literal.EscapingVariables = literal.Elements
                .Select(x => x.Data.AsTypeCheckedData().GetValue())
                .SelectMany(x => x.EscapingVariables)
                .ToImmutableHashSet();

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromFlowAnalyzer(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, TypeCache types, FlowCache flows) {
            var literal = (ArrayLiteralData)data;

            // Delegate flow analysis
            literal.Elements = literal.Elements
                .Select(x => x.AnalyzeFlow(types, flows))
                .ToArray();

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(IFlownData data, ICScope scope, ICodeGenerator gen) {
            var literal = (ArrayLiteralData)data;

            var arrayType = (ArrayType)literal.ReturnType;
            var cArrayType = gen.Generate(arrayType);
            var tempName = "$array_init_" + arrayLiteralCGCounter++;
            var elemType = gen.Generate(arrayType.ElementType);
            var elems = literal.Elements.Select(x => x.GenerateCode(scope, gen)).ToArray();
            var writer = new CWriter();

            foreach (var elem in elems) {
                writer.Lines(elem.SourceLines);
            }

            if (literal.Elements.Any()) {
                writer.Line("// Array initialization");
                writer.VariableInit(cArrayType, tempName);
                writer.Line($"{tempName}.size = {literal.Elements.Count}LL;");
                writer.Line($"{tempName}.data = (uintptr_t)malloc({literal.Elements.Count}LL * sizeof({elemType}));");

                for (int i = 0; i < literal.Elements.Count; i++) {
                    writer.Line($"(({elemType}*)({tempName}.data))[{i}] = {elems[i].Value};");
                }

                writer.Line($"{tempName}.data |= 1;");
            }
            else {
                writer.Line("// Array initialization");
                writer.VariableInit(cArrayType, tempName);
                writer.Line($"{tempName}.size = 0;");
                writer.Line($"{tempName}.data = 0;");
            }

            writer.EmptyLine();

            return writer.ToBlock(tempName);
        }
    }
}