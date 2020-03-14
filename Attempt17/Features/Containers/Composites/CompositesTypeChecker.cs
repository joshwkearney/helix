using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Containers.Composites {
    public class CompositesTypeChecker
        : ICompositesVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> {

        public ISyntax<TypeCheckTag> VisitCompositeDeclaration(
            CompositeDeclarationSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            // Check to make sure that all member types are defined
            foreach (var mem in syntax.CompositeInfo.Signature.Members) {
                if (!mem.Type.IsDefined(context.Scope)) {
                    throw TypeCheckingErrors.TypeUndefined(syntax.Tag.Location,
                        mem.Type.ToFriendlyString());
                }
            }

            // Check to make sure that there are no duplicate member names
            foreach (var mem1 in syntax.CompositeInfo.Signature.Members) {
                foreach (var mem2 in syntax.CompositeInfo.Signature.Members) {
                    if (mem1 != mem2 && mem1.Name == mem2.Name) {
                        throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, mem1.Name);
                    }
                }
            }

            // Check to make sure the struct isn't circular
            if (syntax.CompositeInfo.Type.IsCircular(context.Scope)) {
                throw TypeCheckingErrors.CircularValueObject(syntax.Tag.Location,
                    syntax.CompositeInfo.Type);
            }

            var tag = new TypeCheckTag(VoidType.Instance);

            return new CompositeDeclarationSyntax<TypeCheckTag>(
                tag,
                syntax.CompositeInfo,
                ImmutableList<IDeclaration<TypeCheckTag>>.Empty);
        }

        public ISyntax<TypeCheckTag> VisitCompositeMemberAccess(
            CompositeMemberAccessSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            throw new NotImplementedException();
        }

        public ISyntax<TypeCheckTag> VisitNewComposite(
            NewCompositeSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            // Make sure we're instantiating all of the members
            var instMembers = syntax.Instantiations.Select(x => x.MemberName).ToHashSet();

            var requiredMembers = syntax
                .CompositeInfo
                .Signature
                .Members
                .Select(x => x.Name)
                .ToHashSet();

            var missing = requiredMembers.Except(instMembers);
            var extra = instMembers.Except(requiredMembers);

            // Make sure there are no missing fields
            if (missing.Any()) {
                throw TypeCheckingErrors.NewObjectMissingFields(syntax.Tag.Location,
                    syntax.CompositeInfo.Type, missing);
            }

            // Make sure there are no extra fields
            if (extra.Any()) {
                throw TypeCheckingErrors.NewObjectHasExtraneousFields(syntax.Tag.Location,
                    syntax.CompositeInfo.Type, extra);
            }

            // Type check all the instantiations
            var insts = syntax.Instantiations
                .Select(x => new MemberInstantiation<TypeCheckTag>(x.MemberName,
                    x.Value.Accept(visitor, context)))
                .ToImmutableList();

            // Make sure all instantiations match the correct type
            var zipped = insts
                .Join(
                    syntax.CompositeInfo.Signature.Members,
                    x => x.MemberName,
                    x => x.Name,
                    (x, y) => new {
                        Name = x.MemberName,
                        ActualType = x.Value.Tag.ReturnType,
                        ExpectedType = y.Type
                    })
                .Join(
                    syntax.Instantiations,
                    x => x.Name,
                    x => x.MemberName,
                    (x, y) => new {
                        x.ActualType,
                        x.ExpectedType,
                        x.Name,
                        y.Value.Tag.Location
                    }
                )
                .ToArray();

            foreach (var entry in zipped) {
                if (entry.ActualType != entry.ExpectedType) {
                    throw TypeCheckingErrors.UnexpectedType(entry.Location,
                        entry.ExpectedType, entry.ActualType);
                }
            }

            var captured = insts
                .Select(x => x.Value.Tag.CapturedVariables)
                .Aggregate(ImmutableHashSet<VariableCapture>.Empty, (x, y) => x.Union(y));

            var tag = new TypeCheckTag(syntax.CompositeInfo.Type, captured);

            return new NewCompositeSyntax<TypeCheckTag>(tag, syntax.CompositeInfo, insts);
        }
    }
}
