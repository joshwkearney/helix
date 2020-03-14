using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Containers.Unions {
    public class UnionTypeChecker
        : IUnionVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> {

        public ISyntax<TypeCheckTag> VisitNewUnion(NewUnionSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var member = syntax
                .UnionInfo
                .Signature
                .Members
                .Where(x => x.Name == syntax.Instantiation.MemberName)
                .FirstOrDefault();

            // Make sure the instantiation is in the union
            if (member == null) {
                throw TypeCheckingErrors.MemberUndefined(syntax.Tag.Location,
                    syntax.UnionInfo.Type, member.Name);
            }

            // Type check the instantiaton
            var value = syntax.Instantiation.Value.Accept(visitor, context);

            // Make sure the type of the instantiation matches
            if (member.Type != value.Tag.ReturnType) {
                throw TypeCheckingErrors.UnexpectedType(
                    syntax.Instantiation.Value.Tag.Location,
                    member.Type,
                    value.Tag.ReturnType);
            }

            var inst = new MemberInstantiation<TypeCheckTag>(
                    syntax.Instantiation.MemberName,
                    value);

            var tag = new TypeCheckTag(syntax.UnionInfo.Type, value.Tag.CapturedVariables);

            return new NewUnionSyntax<TypeCheckTag>(tag, syntax.UnionInfo, inst);
        }

        public ISyntax<TypeCheckTag> VisitParseUnionDeclaration(
            ParseUnionDeclarationSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            // Check to make sure that all member types are defined
            foreach (var mem in syntax.UnionInfo.Signature.Members) {
                if (!mem.Type.IsDefined(context.Scope)) {
                    throw TypeCheckingErrors.TypeUndefined(syntax.Tag.Location,
                        mem.Type.ToFriendlyString());
                }
            }

            // Check to make sure that there are no duplicate member or method names
            foreach (var mem1 in syntax.UnionInfo.Signature.Members) {
                foreach (var mem2 in syntax.UnionInfo.Signature.Members) {
                    if (mem1 != mem2 && mem1.Name == mem2.Name) {
                        throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, mem1.Name);
                    }
                }

                foreach (var method in syntax.Methods) {
                    if (mem1.Name == method.Name) {
                        throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, mem1.Name);
                    }
                }
            }

            // Check to make sure the union isn't circular
            if (syntax.UnionInfo.Type.IsCircular(context.Scope)) {
                throw TypeCheckingErrors.CircularValueObject(syntax.Tag.Location,
                    syntax.UnionInfo.Type);
            }

            var parMets = ImmutableDictionary<(Parameter, FunctionSignature), FunctionInfo>.Empty;

            // Make sure that all member types have the methods specified in the union
            foreach (var mem in syntax.UnionInfo.Signature.Members) {
                foreach (var sig in syntax.Methods) {
                    if (!context.Scope.FindMethod(mem.Type, sig.Name).TryGetValue(out var method)) {
                        throw TypeCheckingErrors.MethodNotDefinedOnUnionMember(
                            syntax.Tag.Location,
                            syntax.UnionInfo.Path,
                            sig.Name,
                            mem.Name);
                    }

                    // Remove the "this" parameter from the other method before comparing
                    var firstParam = method.Signature.Parameters.FirstOrDefault();
                    if (firstParam == null || firstParam.Name != "this") {
                        throw TypeCheckingErrors.IncorrectUnionMethodSignature(
                            syntax.Tag.Location,
                            syntax.UnionInfo.Path,
                            sig.Name,
                            mem.Name);
                    }

                    var truncatedMethod = new FunctionSignature(
                        method.Signature.Name,
                        method.Signature.ReturnType,
                        method.Signature.Parameters.RemoveAt(0));

                    if (sig != truncatedMethod) {
                        throw TypeCheckingErrors.IncorrectUnionMethodSignature(
                            syntax.Tag.Location,
                            syntax.UnionInfo.Path,
                            sig.Name,
                            mem.Name);
                    }

                    parMets = parMets.Add((mem, sig), method);
                }
            }

            var tag = new TypeCheckTag(VoidType.Instance);

            return new UnionDeclarationSyntax<TypeCheckTag>(
                tag,
                syntax.UnionInfo,
                syntax.Methods,
                parMets);
        }

        public ISyntax<TypeCheckTag> VisitUnionDeclaration(UnionDeclarationSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            throw new InvalidOperationException();
        }
    }
}
