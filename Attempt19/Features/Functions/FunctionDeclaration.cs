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
        public static Syntax MakeFunctionDeclaration(FunctionSignature sig, Syntax body, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new FunctionDeclarationData() {
                Signature = sig,
                Body = body,
                Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(FunctionDeclarationTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Functions {
    public class FunctionDeclarationData : IParsedData, ITypeCheckedData {
        public FunctionSignature Signature { get; set; }

        public Syntax Body { get; set; }

        public IdentifierPath FunctionPath { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public IdentifierPath BlockPath { get; set; }
    }

    public static class FunctionDeclarationTransformations {
        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope, NameCache names) {
            var func = (FunctionDeclarationData)data;
            var bodyPath = scope.Append(func.Signature.Name).Append("$body");

            // Delegate name declarations
            func.Body = func.Body.DeclareNames(bodyPath, names);

            // Set the containing scope
            func.FunctionPath = scope.Append(func.Signature.Name);

            // Declare this function
            names.AddGlobalName(func.FunctionPath, NameTarget.Function);

            var parNames = func.Signature.Parameters.Select(x => x.Name).ToArray();
            var unique = parNames.Distinct().ToArray();

            // Check for duplicate parameter names
            if (parNames.Length != unique.Length) {
                var dup = parNames.Except(unique).First();

                throw TypeCheckingErrors.IdentifierDefined(func.Location, dup);
            }

            return new Syntax() {
                Data = SyntaxData.From(func),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var func = (FunctionDeclarationData)data;

            names.PushLocalFrame();

            // Declare the parameter names
            for (int i = 0; i < func.Signature.Parameters.Count; i++) {
                var par = func.Signature.Parameters[i];
                var parPath = func.FunctionPath.Append(par.Name);

                // TODO: Reimplement this
                //names.AddLocalName(parPath, NameTarget.Parameter);
            }

            // Delegate name resolutions
            func.Body = func.Body.ResolveNames(names);

            names.PopLocalFrame();

            return new Syntax() {
                Data = SyntaxData.From(func),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var func = (FunctionDeclarationData)data;

            // Declare this function
            types.Functions[func.FunctionPath] = func.Signature;

            // TODO: Reimplement this
            // Declare the parameters
            //for (int i = 0; i < func.Signature.Parameters.Count; i++) {
            //    var par = func.Signature.Parameters[i];
            //    var parPath = func.FunctionPath.Append(par.Name);
            //    var capPath = func.FunctionPath.Append("$par" + i).Append(par.Name);
            //    var info = new ParameterInfo(par.Type, capPath);

            //    types.Parameter[parPath] = info;
            //}

            // Delegate type declarations
            func.Body = func.Body.DeclareTypes(types);

            return new Syntax() {
                Data = SyntaxData.From(func),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var func = (FunctionDeclarationData)data;

            // Delegate type resolutions
            func.Body = func.Body.ResolveTypes(types, unifier);

            // Set the return type
            func.ReturnType = VoidType.Instance;

            // Set no escaping variables
            func.Lifetimes = ImmutableHashSet.Create<IdentifierPath>();

            var body = func.Body.Data.AsTypeCheckedData().GetValue();

            // Make sure the body's return type matches the function return type
            if (func.Signature.ReturnType != body.ReturnType) {
                throw TypeCheckingErrors.UnexpectedType(body.Location, 
                    func.Signature.ReturnType, body.ReturnType);
            }

            // The return value must be allocated on the heap
            var heapLifetime = new IdentifierPath("heap");

            foreach (var capLifetime in body.Lifetimes) {
                if (capLifetime != heapLifetime) {
                    throw TypeCheckingErrors.LifetimeExceeded(body.Location, heapLifetime, capLifetime);
                }
            }

            return new Syntax() {
                Data = SyntaxData.From(func),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(ITypeCheckedData data, ICodeGenerator gen) {
            var func = (FunctionDeclarationData)data;

            var writer = new CWriter();
            var body = func.Body.GenerateCode(gen);
            var line = GenerateSignature(func.Signature, func.FunctionPath, gen, true) + " {";

            writer.Line(line);
            writer.Lines(CWriter.Indent(body.SourceLines));
            writer.Lines(CWriter.Indent($"return {body.Value};"));
            writer.Line("}");
            writer.EmptyLine();

            gen.Header2Writer
                .Line(GenerateSignature(func.Signature, func.FunctionPath, gen, true) + ";")
                .EmptyLine();

            return writer.ToBlock("0");
        }

        private static string GenerateSignature(FunctionSignature sig, IdentifierPath path, ICodeGenerator gen, bool includeHeap) {
            var line = "";

            line += gen.Generate(sig.ReturnType) + " ";
            line += path.ToString();
            line += "(";

            if (includeHeap) {
                line += "$Region* $reg_heap";
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