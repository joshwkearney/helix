using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Features.Containers.Arrays;
using Attempt17.Features.Containers.Composites;
using Attempt17.Features.Containers.Unions;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Containers {
    public class ContainersTypeChecker
        : IContainersVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> {

        public
        IArraysVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> ArraysVisitor { get; }
                = new ArraysTypeChecker();

        public
        ICompositesVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> CompositesVisitor
            { get; } = new CompositesTypeChecker();

        public
        IUnionVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> UnionVisitor { get; }
             = new UnionTypeChecker();

        public ISyntax<TypeCheckTag> VisitMemberUsage(MemberUsageSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            IMemberAccessTarget initial = new ValueMemberAccessTarget(
                syntax.Target.Accept(visitor, context),
                syntax.Tag.Location,
                context.Scope);

            foreach (var seg in syntax.UsageSegments) {
                initial = seg.Match(
                    access => initial.AccessMember(access.MemberName),
                    invoke => {
                        var args = invoke.Arguments
                            .Select(x => x.Accept(visitor, context))
                            .ToImmutableList();

                        return initial.InvokeMember(invoke.MemberName, args);
                    });
            }

            return initial.ToSyntax();
        }

        public ISyntax<TypeCheckTag> VisitNew(NewSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            // Make sure the type is a namedType
            if (!(syntax.Type is NamedType namedType)) {
                throw TypeCheckingErrors.ExpectedStructType(syntax.Tag.Location, syntax.Type);
            }

            if (context.Scope.FindComposite(namedType.Path).TryGetValue(out var structInfo)) {
                bool isClassOrStruct = structInfo.Kind == CompositeKind.Class
                    || structInfo.Kind == CompositeKind.Struct;

                if (isClassOrStruct) {
                    var newSyntax = new NewCompositeSyntax<ParseTag>(syntax.Tag, structInfo,
                        syntax.Instantiations);

                    return newSyntax.Accept(visitor, context);
                }
                else if (structInfo.Kind == CompositeKind.Union) {
                    // Unions can only have one instantiation
                    if (!syntax.Instantiations.Any()) {
                        throw TypeCheckingErrors.NewObjectMissingFields(
                            syntax.Tag.Location,
                            syntax.Type,
                            structInfo.Signature.Members.Select(x => x.Name));
                    }

                    if (syntax.Instantiations.Count() > 1) {
                        throw TypeCheckingErrors.NewObjectHasExtraneousFields(
                            syntax.Tag.Location,
                            syntax.Type,
                            syntax
                                .Instantiations
                                .Select(x => x.MemberName)
                                .Except(structInfo.Signature.Members.Select(x => x.Name)));
                    }

                    var newSyntax = new NewUnionSyntax<ParseTag>(syntax.Tag, structInfo,
                        syntax.Instantiations[0]);

                    return newSyntax.Accept(visitor, context);
                }
                else {
                    throw new Exception();
                }
            }

            throw TypeCheckingErrors.UnexpectedType(syntax.Tag.Location, namedType);
        }
    }
}
