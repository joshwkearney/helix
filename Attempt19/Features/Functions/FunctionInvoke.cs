using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt19.TypeChecking;
using Attempt19;
using Attempt19.CodeGeneration;
using Attempt19.Parsing;
using Attempt19.Types;
using Attempt19.Features.Functions;
using Attempt19.Features.FlowControl;
using System;

namespace Attempt19 {
    public static partial class SyntaxFactory {
        public static Syntax MakeFunctionInvoke(Syntax target, IReadOnlyList<Syntax> args, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new FunctionInvokeData() {
                    Target = target,
                    Arguments = args,
                    Location = loc
                }),
                Operator = SyntaxOp.FromNameDeclarator(FunctionInvokeTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Functions {
    public class FunctionInvokeData : IParsedData, ITypeCheckedData {
        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public Syntax Target { get; set; }

        public IReadOnlyList<Syntax> Arguments { get; set; }

        public FunctionSignature TargetSignature { get; set; }
    }

    public static class FunctionInvokeTransformations {
        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope, NameCache names) {
            var func = (FunctionInvokeData)data;

            // Delegate name declarations
            func.Target = func.Target.DeclareNames(scope, names);
            func.Arguments = func.Arguments.Select(x => x.DeclareNames(scope, names)).ToArray();

            return new Syntax() {
                Data = SyntaxData.From(func),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var func = (FunctionInvokeData)data;

            // Delegate name resolution
            func.Target = func.Target.ResolveNames(names);
            func.Arguments = func.Arguments.Select(x => x.ResolveNames(names)).ToArray();

            return new Syntax() {
                Data = SyntaxData.From(func),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var func = (FunctionInvokeData)data;

            // Delegate type declaration
            func.Target = func.Target.DeclareTypes(types);
            func.Arguments = func.Arguments.Select(x => x.DeclareTypes(types)).ToArray();

            return new Syntax() {
                Data = SyntaxData.From(func),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var func = (FunctionInvokeData)data;

            // Delegate type resolutions
            func.Target = func.Target.ResolveTypes(types, unifier);
            func.Arguments = func.Arguments.Select(x => x.ResolveTypes(types, unifier)).ToArray();

            var type = func.Target.Data.AsTypeCheckedData().GetValue().ReturnType;

            // Make sure the target's type is a function
            if (type is not FunctionType funcType) {
                throw TypeCheckingErrors.ExpectedFunctionType(func.Location, type);
            }

            var funcInfo = types.Functions[funcType.FunctionPath];

            // Set the signature
            func.TargetSignature = funcInfo;

            // Make sure the parameter counts match
            if (funcInfo.Parameters.Count != func.Arguments.Count) {
                throw TypeCheckingErrors.ParameterCountMismatch(func.Location, funcInfo.Parameters.Count, func.Arguments.Count);
            }

            // TODO - make sure the given heap outlives all of the parameters

            // Set the return type
            func.ReturnType = funcInfo.ReturnType;

            // Set the escaping variables
            // TODO - make this the given heap
            func.Lifetimes = new[] { new IdentifierPath("heap") }.ToImmutableHashSet();

            return new Syntax() {
                Data = SyntaxData.From(func),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(ITypeCheckedData data, ICodeGenerator gen) {
            var func = (FunctionInvokeData)data;

            var writer = new CWriter();
            var args = func.Arguments.Select(x => x.GenerateCode(gen)).ToArray();

            writer.Lines(args.SelectMany(x => x.SourceLines));

            return new CBlock(func.)
        }

        private static string GenerateSignature(FunctionSignature sig, IdentifierPath path, ICodeGenerator gen, bool includeHeap) {
            var line = "";

            line += gen.Generate(sig.ReturnType) + " ";
            line += path.ToString();
            line += "(";

            if (includeHeap) {
                line += "$Region* $reg_heap";

                if (sig.Parameters.Any()) {
                    line += ", ";
                }
            }

            foreach (var par in sig.Parameters) {
                line += gen.Generate(par.Type) + " ";
                line += par.Name + ", ";
            }

            line = line.TrimEnd(' ', ',');
            line += ")";

            return line;
        }
    }
}