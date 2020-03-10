using System;
using Attempt17.Features.Functions;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Structs {
    public class StructsTypeChecker {
        public void ModifyScopeForStructDeclaration(StructDeclarationParseTree syntax, IScope scope) {
            var path = scope.Path.Append(syntax.Signature.Name);

            // Check to make sure the name isn't taken
            if (scope.IsPathTaken(path)) {
                throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, syntax.Signature.Name);
            }

            scope.SetTypeInfo(path, new StructInfo(syntax.Signature, path));
        }

        public ISyntax<TypeCheckTag> CheckStructDeclaration(StructDeclarationParseTree syntax, IScope scope, ITypeChecker checker) {
            var path = scope.Path.Append(syntax.Signature.Name);
            var info = scope.FindStruct(path).GetValue();

            // Check to make sure that there are no duplicate member names
            foreach (var mem1 in syntax.Signature.Members) {
                foreach (var mem2 in syntax.Signature.Members) {
                    if (mem1 != mem2 && mem1.Name == mem2.Name) {
                        throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, mem1.Name);
                    }
                }
            }

            // Check to make sure that all member types are defined
            foreach (var mem in syntax.Signature.Members) {
                if (!checker.IsTypeDefined(mem.Type, scope)) {
                    throw TypeCheckingErrors.TypeUndefined(syntax.Tag.Location, mem.Type.ToFriendlyString());
                }
            }

            var tag = new TypeCheckTag(VoidType.Instance);

            return new StructDeclarationSyntaxTree(
                tag,
                info);
        }
    }
}