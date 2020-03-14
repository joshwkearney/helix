using System;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Containers.Unions {
    public class UnionTypeChecker
        : IUnionVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> {

        public ISyntax<TypeCheckTag> VisitNewUnion(NewUnionSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            throw new NotImplementedException();
        }

        public ISyntax<TypeCheckTag> VisitUnionDeclaration(UnionDeclarationSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            // Check to make sure that there are no duplicate member or method names
            foreach (var mem1 in syntax.UnionInfo.Signature.Members) {
                foreach (var mem2 in syntax.UnionInfo.Signature.Members) {
                    if (mem1 != mem2 && mem1.Name == mem2.Name) {
                        throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, mem1.Name);
                    }
                }
            }

            // Check to make sure the union isn't circular
            if (syntax.UnionInfo.Type.IsCircular(context.Scope)) {
                throw TypeCheckingErrors.CircularValueObject(syntax.Tag.Location, syntax.UnionInfo.Type);
            }

            // Check to make sure that all member types are defined
            foreach (var mem in syntax.UnionInfo.Signature.Members) {
                if (!mem.Type.IsDefined(context.Scope)) {
                    throw TypeCheckingErrors.TypeUndefined(syntax.Tag.Location, mem.Type.ToFriendlyString());
                }
            }

            // Make sure that all member types have the methods specified in the union
            foreach (var mem in syntax.UnionInfo.Signature.Members) {
                foreach (var sig in syntax.Methods) {
                    if (!context.Scope.FindMethod(mem.Type, sig.Name).TryGetValue(out var method)) {
                        throw new Exception();
                    }

                    if (sig != method.Signature) {
                        throw new Exception();
                    }
                }
            }

            var tag = new TypeCheckTag(VoidType.Instance);

            return new UnionDeclarationSyntax<TypeCheckTag>(
                tag,
                syntax.UnionInfo,
                syntax.Methods);
        }
    }
}
