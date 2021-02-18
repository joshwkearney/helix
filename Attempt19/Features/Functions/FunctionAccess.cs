using System.Collections.Immutable;
using Attempt19.TypeChecking;
using Attempt19.CodeGeneration;
using Attempt19.Parsing;
using Attempt19.Types;
using Attempt19.Features.Variables;
using System.Linq;
using System;

namespace Attempt19.Features.Variables {
    public class FunctionAccessData : IParsedData, ITypeCheckedData { 
        public IdentifierPath FunctionPath { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }
    }

    public static class FunctionAccessTransformations {
        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var access = (VariableAccessData)data;

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var access = (FunctionAccessData)data;

            // Set return type
            access.ReturnType = new FunctionType(access.FunctionPath);

            // Set variable lifetimes
            access.Lifetimes = ImmutableHashSet.Create<IdentifierPath>();

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(ITypeCheckedData data, ICodeGenerator gen) {
            throw new NotImplementedException();
        }
    }
}