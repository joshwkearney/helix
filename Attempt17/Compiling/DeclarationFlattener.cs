using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Features.Functions;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Compiling {
    public class DeclarationFlattener : IParseDeclarationVisitor<IEnumerable<IParseDeclaration>> {
        public IEnumerable<IParseDeclaration> VisitFunctionDeclaration(ParseFunctionDeclaration decl) {
            return new[] { decl };
        }

        public IEnumerable<IParseDeclaration> VisitStructDeclaration(ParseStructDeclaration decl) {
            var transformer = new StructDeclarationTransformer(decl.StructInfo.Path);

            var newDecl = new ParseStructDeclaration(
                decl.Tag,
                decl.StructInfo,
                ImmutableList<IParseDeclaration>.Empty);

            return decl
                .Declarations
                .SelectMany(x => x.Accept(transformer).Accept(this))
                .Append(newDecl)
                .ToArray();
        }
    }

    public class StructDeclarationTransformer : IParseDeclarationVisitor<IParseDeclaration> {
        private readonly IdentifierPath containingStruct;

        public StructDeclarationTransformer(IdentifierPath containingStruct) {
            this.containingStruct = containingStruct;
        }

        public IParseDeclaration VisitFunctionDeclaration(ParseFunctionDeclaration decl) {
            var firstParam = new FunctionParameter("this", new NamedType(this.containingStruct));

            var newSig = new FunctionSignature(
                decl.FunctionInfo.Signature.Name,
                decl.FunctionInfo.Signature.ReturnType,
                decl.FunctionInfo.Signature.Parameters.Insert(0, firstParam));

            var newInfo = new FunctionInfo(
                this.containingStruct.Append(decl.FunctionInfo.Path),
                newSig);

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