using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Features.Functions;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Structs {
    public class StructsTypeChecker {
        public void ModifyScopeForStructDeclaration(ParseStructDeclaration syntax, IScope scope) {
            // Check to make sure the name isn't taken
            if (scope.IsPathTaken(syntax.StructInfo.Path)) {
                throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, syntax.StructInfo.Signature.Name);
            }

            scope.SetTypeInfo(syntax.StructInfo.Path, syntax.StructInfo);
        }

        public ISyntax<TypeCheckTag> CheckStructDeclaration(ParseStructDeclaration syntax, IScope scope, ITypeChecker checker) {
            // Check to make sure that there are no duplicate member names
            foreach (var mem1 in syntax.StructInfo.Signature.Members) {
                foreach (var mem2 in syntax.StructInfo.Signature.Members) {
                    if (mem1 != mem2 && mem1.Name == mem2.Name) {
                        throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, mem1.Name);
                    }
                }
            }

            // Check to make sure that all member types are defined
            foreach (var mem in syntax.StructInfo.Signature.Members) {
                if (!checker.IsTypeDefined(mem.Type, scope)) {
                    throw TypeCheckingErrors.TypeUndefined(syntax.Tag.Location, mem.Type.ToFriendlyString());
                }
            }

            var tag = new TypeCheckTag(VoidType.Instance);

            return new StructDeclarationSyntaxTree(
                tag,
                syntax.StructInfo);
        }

        public ISyntax<TypeCheckTag> CheckNew(NewSyntax<ParseTag> syntax, IScope scope, ITypeChecker checker) {
            // Make sure the type is a namedType
            if (!(syntax.Type is NamedType namedType)) {
                throw TypeCheckingErrors.ExpectedStructType(syntax.Tag.Location, syntax.Type);
            }

            // Make sure the type is a struct type
            if (!scope.FindStruct(namedType.Path).TryGetValue(out var structInfo)) {
                throw TypeCheckingErrors.ExpectedStructType(syntax.Tag.Location, syntax.Type);
            }

            // Make sure we're instantiating all of the members
            var instMembers = syntax.Instantiations.Select(x => x.MemberName).ToHashSet();
            var requiredMembers = structInfo.Signature.Members.Select(x => x.Name).ToHashSet();

            var missing = requiredMembers.Except(instMembers);
            var extra = instMembers.Except(requiredMembers);

            // Make sure there are no missing fields
            if (missing.Any()) {
                throw TypeCheckingErrors.NewObjectMissingFields(syntax.Tag.Location, structInfo.StructType, missing);
            }

            // Make sure there are no extra fields
            if (extra.Any()) {
                throw TypeCheckingErrors.NewObjectHasExtraneousFields(syntax.Tag.Location, structInfo.StructType, extra);
            }

            // Type check all the instantiations
            var insts = syntax.Instantiations
                .Select(x => new MemberInstantiation<TypeCheckTag>(x.MemberName, checker.Check(x.Value, scope)))
                .ToImmutableList();

            var captured = insts
                .Select(x => x.Value.Tag.CapturedVariables)
                .Aggregate(ImmutableHashSet<VariableCapture>.Empty, (x, y) => x.Union(y));

            var tag = new TypeCheckTag(structInfo.StructType, captured);

            return new NewSyntax<TypeCheckTag>(tag, syntax.Type, insts);
        }

        public ISyntax<TypeCheckTag> CheckMemberUsage(MemberUsageParseSyntax syntax, IScope scope, ITypeChecker checker) {
            IMemberAccessTarget initial = new ValueMemberAccessTarget(
                checker.Check(syntax.Target, scope),
                syntax.Tag.Location,
                scope);

            foreach (var seg in syntax.UsageSegments) {
                initial = seg.Match(
                    access => initial.AccessMember(access.MemberName),
                    invoke => throw new NotImplementedException());
            }

            return initial.ToSyntax();
        }
    }
}