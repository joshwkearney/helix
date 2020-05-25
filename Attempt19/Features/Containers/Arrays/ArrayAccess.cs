using System.Collections.Immutable;
using Attempt19.TypeChecking;
using Attempt19.CodeGeneration;
using Attempt19.Parsing;
using Attempt19.Types;
using System.Linq;
using System.Collections.Generic;
using Attempt19.Features.Containers.Arrays;
using System;
using Attempt19.Features.Variables;

namespace Attempt19 {
    public static partial class SyntaxFactory {
        public static Syntax MakeArrayAccess(string target, Syntax index, ArrayAccessKind kind, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new ArrayAccessData() {
                    VariableName = target,
                    IndexSyntax = index,
                    Kind = kind,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(ArrayAccessTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Containers.Arrays {
    public enum ArrayAccessKind {
        ValueAccess, LiteralAccess
    }

    public class ArrayAccessData : VariableAccessBase {
        public ArrayAccessKind Kind { get; set; }

        public Syntax IndexSyntax { get; set; }

        public ArrayType ArrayType { get; set; }
    }    

    public static class ArrayAccessTransformations {
        private static int arrayAccessCounter = 0;

        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope, NameCache names) {
            var index = (ArrayAccessData)data;

            // Delegate name declaration
            index.IndexSyntax = index.IndexSyntax.DeclareNames(scope, names);

            // Set containing nscope
            index.ContainingScope = scope;

            return new Syntax() {
                Data = SyntaxData.From(index),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var index = (ArrayAccessData)data;

            // Delegate name resolution
            index.IndexSyntax = index.IndexSyntax.ResolveNames(names);

            // Make sure this name exists
            if (!names.FindName(index.ContainingScope, index.VariableName, out var varpath, out var target)) {
                throw TypeCheckingErrors.VariableUndefined(index.Location, index.VariableName);
            }

            // Make sure this name is a variable
            if (target != NameTarget.Variable) {
                throw TypeCheckingErrors.VariableUndefined(index.Location, index.VariableName);
            }

            // Set target path
            index.VariablePath = varpath;

            return new Syntax() {
                Data = SyntaxData.From(index),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var index = (ArrayAccessData)data;

            // Delegate type declaration
            index.IndexSyntax = index.IndexSyntax.DeclareTypes(types);

            return new Syntax() {
                Data = SyntaxData.From(index),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var index = (ArrayAccessData)data;

            // Delegate type resolution
            index.IndexSyntax = index.IndexSyntax.ResolveTypes(types, unifier);

            var targetType = types.Variables[index.VariablePath].Type;
            var indexSyntax = index.IndexSyntax.Data.AsTypeCheckedData().GetValue();

            // Make sure the target is an array type
            if (!(targetType is ArrayType arrType)) {
                throw TypeCheckingErrors.ExpectedArrayType(
                    index.Location, targetType);
            }

            // Set array type
            index.ArrayType = arrType;

            // Make sure the index is an int type
            if (!(indexSyntax.ReturnType is IntType)) {
                throw TypeCheckingErrors.UnexpectedType(
                    indexSyntax.Location, IntType.Instance, indexSyntax.ReturnType);
            }

            // Set return type
            if (index.Kind == ArrayAccessKind.ValueAccess) {
                index.ReturnType = arrType.ElementType;
            }
            else {
                index.ReturnType = new VariableType(arrType.ElementType);
            }

            // Set escaping variables
            if (index.Kind == ArrayAccessKind.LiteralAccess) {
                var cap = new VariableCapture(VariableCaptureKind.ValueCapture, index.VariablePath);
                index.EscapingVariables = new[] { cap }.ToImmutableHashSet();
            }
            else if (index.ArrayType.ElementType.GetCopiability() == TypeCopiability.Conditional) {
                var cap = new VariableCapture(VariableCaptureKind.ValueCapture, index.VariablePath);
                index.EscapingVariables = new[] { cap }.ToImmutableHashSet();
            }
            else {
                index.EscapingVariables = ImmutableHashSet.Create<VariableCapture>();
            }

            return new Syntax() {
                Data = SyntaxData.From(index),
                Operator = SyntaxOp.FromFlowAnalyzer(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, TypeCache types, FlowCache flows) {
            var index = (ArrayAccessData)data;

            // Delegate flow analysis
            index.IndexSyntax = index.IndexSyntax.AnalyzeFlow(types, flows);

            return new Syntax() {
                Data = SyntaxData.From(index),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(IFlownData data, ICScope scope, ICodeGenerator gen) {
            var index = (ArrayAccessData)data;

            var cIndex = index.IndexSyntax.GenerateCode(scope, gen);
            var cElemType = gen.Generate(index.ArrayType.ElementType);
            var varname = "$array_access_" + arrayAccessCounter++;
            var writer = new CWriter();

            writer.Lines(cIndex.SourceLines);
            writer.Line("// Array access");

            string elemPtr = $"({index.VariableName}.data & ~1) + {cIndex.Value} * sizeof({cElemType})";

            if (index.Kind == ArrayAccessKind.ValueAccess) {
                var value = gen.CopyValue($"*({elemPtr})", index.ReturnType, scope);
                writer.Lines(value.SourceLines);

                return writer.ToBlock(value.Value);
            }
            else {
                writer.VariableInit(gen.Generate(index.ReturnType), varname, elemPtr);
                writer.EmptyLine();

                return writer.ToBlock(varname);
            }
        }
    }
}