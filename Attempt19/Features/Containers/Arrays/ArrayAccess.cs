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
        public static Syntax MakeArrayAccess(Syntax target, Syntax index, ArrayAccessKind kind, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new ArrayAccessData() {
                    TargetSyntax = target,
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

    public class ArrayAccessData : IParsedData, ITypeCheckedData {
        public ArrayAccessKind Kind { get; set; }

        public Syntax TargetSyntax { get; set; }

        public Syntax IndexSyntax { get; set; }

        public ArrayType ArrayType { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }
    }    

    public static class ArrayAccessTransformations {
        private static int arrayAccessCounter = 0;

        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope, NameCache names) {
            var index = (ArrayAccessData)data;

            // Delegate name declaration
            index.TargetSyntax = index.TargetSyntax.DeclareNames(scope, names);
            index.IndexSyntax = index.IndexSyntax.DeclareNames(scope, names);

            return new Syntax() {
                Data = SyntaxData.From(index),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var index = (ArrayAccessData)data;

            // Delegate name resolution
            index.TargetSyntax = index.TargetSyntax.ResolveNames(names);
            index.IndexSyntax = index.IndexSyntax.ResolveNames(names);

            return new Syntax() {
                Data = SyntaxData.From(index),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var index = (ArrayAccessData)data;

            // Delegate type declaration
            index.TargetSyntax = index.TargetSyntax.DeclareTypes(types);
            index.IndexSyntax = index.IndexSyntax.DeclareTypes(types);

            return new Syntax() {
                Data = SyntaxData.From(index),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var index = (ArrayAccessData)data;

            // Delegate type resolution
            index.TargetSyntax = index.TargetSyntax.ResolveTypes(types, unifier);
            index.IndexSyntax = index.IndexSyntax.ResolveTypes(types, unifier);

            var target = index.TargetSyntax.Data.AsTypeCheckedData().GetValue();
            var indexSyntax = index.IndexSyntax.Data.AsTypeCheckedData().GetValue();

            // Make sure the target is an array type
            if (!(target.ReturnType is ArrayType arrType)) {
                throw TypeCheckingErrors.ExpectedArrayType(
                    index.Location, target.ReturnType);
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
            if (index.ArrayType.ElementType.GetCopiability() == TypeCopiability.Unconditional) {
                index.Lifetimes = ImmutableHashSet.Create<IdentifierPath>();
            }
            else {
                index.Lifetimes = target.Lifetimes;
            }

            return new Syntax() {
                Data = SyntaxData.From(index),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(ITypeCheckedData data, ICodeGenerator gen) {
            var index = (ArrayAccessData)data;

            var cTarget = index.TargetSyntax.GenerateCode(gen);
            var cIndex = index.IndexSyntax.GenerateCode(gen);
            var cElemType = gen.Generate(index.ArrayType.ElementType);
            var varname = "$array_access_" + arrayAccessCounter++;
            var writer = new CWriter();

            writer.Lines(cTarget.SourceLines);
            writer.Lines(cIndex.SourceLines);
            writer.Line("// Array access");

            // Write pointer addition
            string elemPtr = $"({cTarget.Value}.data + {cIndex.Value} * sizeof({cElemType}))";
            writer.VariableInit(gen.Generate(new VariableType(index.ArrayType.ElementType)), varname, elemPtr);

            // Write bounds check
            writer.Line($"if ({cIndex.Value} < 0 || {cIndex.Value} >= {cTarget.Value}.size) {{");
            writer.Lines(CWriter.Indent("fprintf(stderr, \"Array bounds check failed\\n\");"));
            writer.Lines(CWriter.Indent("exit(-1);"));
            writer.Line("}");
            writer.EmptyLine();

            if (index.Kind == ArrayAccessKind.ValueAccess) {
                return writer.ToBlock("(*" + varname + ")");
            }
            else {
                return writer.ToBlock(varname);
            }
        }
    }
}