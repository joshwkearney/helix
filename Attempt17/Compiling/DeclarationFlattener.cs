using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Features.Functions;
using Attempt17.Features.Structs;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Compiling {
    public class DeclarationFlattener : IParseDeclarationVisitor<IEnumerable<ISyntax<ParseTag>>> {
        private readonly IScope scope;

        public DeclarationFlattener(IScope scope) {
            this.scope = scope;
        }

        public IEnumerable<ISyntax<ParseTag>> VisitFunctionDeclaration(ParseFunctionDeclaration decl) {
            return new[] { new FunctionDeclarationSyntax<ParseTag>(decl.Tag, decl.FunctionInfo, decl.Body) };
        }

        public IEnumerable<ISyntax<ParseTag>> VisitStructDeclaration(ParseStructDeclaration decl) {
            var transformer = new StructDeclarationTransformer(decl.StructInfo.Path, this.scope);

            var newDecl = new StructDeclarationSyntax<ParseTag>(
                decl.Tag,
                decl.StructInfo);

            return decl
                .Declarations
                .SelectMany(x => x.Accept(transformer).Accept(this))
                .Append(newDecl)
                .ToArray();
        }
    }

    public class StructDeclarationTransformer : IParseDeclarationVisitor<IParseDeclaration> {
        private readonly IdentifierPath containingStruct;
        private readonly IScope scope;

        public StructDeclarationTransformer(IdentifierPath containingStruct, IScope scope) {
            this.containingStruct = containingStruct;
            this.scope = scope;
        }

        public IParseDeclaration VisitFunctionDeclaration(ParseFunctionDeclaration decl) {
            var structType = new NamedType(this.containingStruct);
            var firstParam = new FunctionParameter("this", structType);

            var newSig = new FunctionSignature(
                decl.FunctionInfo.Signature.Name,
                decl.FunctionInfo.Signature.ReturnType,
                decl.FunctionInfo.Signature.Parameters.Insert(0, firstParam));

            var newInfo = new FunctionInfo(
                this.containingStruct.Append(decl.FunctionInfo.Path),
                newSig);

            this.scope.SetMethod(structType, decl.FunctionInfo.Signature.Name, newInfo.Path);

            return new ParseFunctionDeclaration(decl.Tag, newInfo, decl.Body);
        }

        public IParseDeclaration VisitStructDeclaration(ParseStructDeclaration decl) {
            var newInfo = new StructInfo(
                decl.StructInfo.Signature,
                this.containingStruct.Append(decl.StructInfo.Path));

            return new ParseStructDeclaration(decl.Tag, newInfo, decl.Declarations);
        }
    }
}