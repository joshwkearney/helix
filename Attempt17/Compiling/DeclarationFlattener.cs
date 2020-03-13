using System.Collections.Generic;
using System.Linq;
using Attempt17.Features.Containers.Composites;
using Attempt17.Features.Functions;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Compiling {
    public class DeclarationFlattener : IParseDeclarationVisitor<IEnumerable<ISyntax<ParseTag>>> {
        private readonly ITypeCheckScope scope;

        public DeclarationFlattener(ITypeCheckScope scope) {
            this.scope = scope;
        }

        public IEnumerable<ISyntax<ParseTag>> VisitFunctionDeclaration(ParseFunctionDeclaration decl) {
            return new[] { new FunctionDeclarationSyntax<ParseTag>(decl.Tag, decl.FunctionInfo, decl.Body) };
        }

        public IEnumerable<ISyntax<ParseTag>> VisitCompositeDeclaration(ParseCompositeDeclaration decl) {
            var transformer = new StructDeclarationTransformer(decl.CompositeInfo.Path, this.scope);

            var newDecl = new CompositeDeclarationSyntax<ParseTag>(
                decl.Tag,
                decl.CompositeInfo);

            return decl
                .Declarations
                .SelectMany(x => x.Accept(transformer).Accept(this))
                .Prepend(newDecl)
                .ToArray();
        }
    }

    public class StructDeclarationTransformer : IParseDeclarationVisitor<IParseDeclaration> {
        private readonly IdentifierPath containingStruct;
        private readonly ITypeCheckScope scope;

        public StructDeclarationTransformer(IdentifierPath containingStruct, ITypeCheckScope scope) {
            this.containingStruct = containingStruct;
            this.scope = scope;
        }

        public IParseDeclaration VisitFunctionDeclaration(ParseFunctionDeclaration decl) {
            var structType = new NamedType(this.containingStruct);
            var firstParam = new Parameter("this", structType);

            var newSig = new FunctionSignature(
                decl.FunctionInfo.Signature.Name,
                decl.FunctionInfo.Signature.ReturnType,
                decl.FunctionInfo.Signature.Parameters.Insert(0, firstParam));

            var newInfo = new FunctionInfo(
                this.containingStruct.Append(decl.FunctionInfo.Path), newSig);

            this.scope.SetMethod(structType, decl.FunctionInfo.Signature.Name, newInfo.Path);

            return new ParseFunctionDeclaration(decl.Tag, newInfo, decl.Body);
        }

        public IParseDeclaration VisitCompositeDeclaration(ParseCompositeDeclaration decl) {
            var newInfo = new CompositeInfo(
                decl.CompositeInfo.Signature,
                this.containingStruct.Append(decl.CompositeInfo.Path),
                decl.CompositeInfo.Kind);

            return new ParseCompositeDeclaration(decl.Tag, newInfo, decl.Declarations);
        }
    }
}