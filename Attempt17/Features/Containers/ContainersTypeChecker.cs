using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Features.Containers.Structs;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Containers {
    public class ContainersTypeChecker {
        public ISyntax<TypeCheckTag> CheckNew(NewSyntax syntax, IScope scope, ITypeChecker checker) {
            // Make sure the type is a namedType
            if (!(syntax.Type is NamedType namedType)) {
                throw TypeCheckingErrors.ExpectedStructType(syntax.Tag.Location, syntax.Type);
            }

            if (scope.FindStruct(namedType.Path).TryGetValue(out var structInfo)) {
                return checker.Check(new NewStructSyntax<ParseTag>(syntax.Tag, structInfo, syntax.Instantiations), scope);
            }

            throw TypeCheckingErrors.UnexpectedType(syntax.Tag.Location, namedType);
        }

        public ISyntax<TypeCheckTag> CheckMemberUsage(MemberUsageParseSyntax syntax, IScope scope, ITypeChecker checker) {
            IMemberAccessTarget initial = new ValueMemberAccessTarget(
                checker.Check(syntax.Target, scope),
                syntax.Tag.Location,
                scope);

            foreach (var seg in syntax.UsageSegments) {
                initial = seg.Match(
                    access => initial.AccessMember(access.MemberName),
                    invoke => {
                        var args = invoke.Arguments
                            .Select(x => checker.Check(x, scope))
                            .ToImmutableList();

                        return initial.InvokeMember(invoke.MemberName, args);
                    });
            }

            return initial.ToSyntax();
        }
    }
}