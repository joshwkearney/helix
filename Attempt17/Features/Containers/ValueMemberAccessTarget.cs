using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Features.Arrays;
using Attempt17.Features.Containers.Composites;
using Attempt17.Features.Functions;
using Attempt17.Features.Variables;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Containers {
    public class ValueMemberAccessTarget : IMemberAccessTarget {
        private readonly ISyntax<TypeCheckTag> value;
        private readonly ITypeCheckScope scope;
        private readonly TokenLocation loc;

        public ValueMemberAccessTarget(ISyntax<TypeCheckTag> value, TokenLocation loc, ITypeCheckScope scope) {
            this.value = value;
            this.scope = scope;
            this.loc = loc;
        }

        public IMemberAccessTarget AccessMember(string name) {
            var value = this.value.Tag.ReturnType.Accept(new MemberAccessVisitor(name, this.loc, this.value, this.scope));

            return new ValueMemberAccessTarget(value, this.loc, this.scope);
        }

        public IMemberAccessTarget InvokeMember(string name, ImmutableList<ISyntax<TypeCheckTag>> arguments) {
            if (!this.scope.FindMethod(this.value.Tag.ReturnType, name).TryGetValue(out var info)) {
                throw TypeCheckingErrors.MemberUndefined(this.loc, this.value.Tag.ReturnType, name);
            }

            var target = new FunctionLiteralSyntax(new TypeCheckTag(info.FunctionType));

            var access = FunctionsTypeChecker.CheckFunctionInvoke(
                this.loc,
                target,
                arguments.Insert(0, this.value),
                this.scope);

            return new ValueMemberAccessTarget(access, this.loc, this.scope);
        }

        public ISyntax<TypeCheckTag> ToSyntax() {
            return this.value;
        }

        private class MemberAccessVisitor : ITypeVisitor<ISyntax<TypeCheckTag>> {
            private readonly string memberName;
            private readonly ITypeCheckScope scope;
            private readonly TokenLocation location;
            private readonly ISyntax<TypeCheckTag> target;

            public MemberAccessVisitor(string name, TokenLocation loc, ISyntax<TypeCheckTag> target, ITypeCheckScope scope) {
                this.memberName = name;
                this.scope = scope;
                this.location = loc;
                this.target = target;
            }

            public ISyntax<TypeCheckTag> VisitArrayType(ArrayType type) {
                if (this.memberName != "size") {
                    throw TypeCheckingErrors.MemberUndefined(
                        this.location,
                        type,
                        this.memberName);
                }

                var tag = new TypeCheckTag(IntType.Instance);

                return new ArraySizeAccessSyntax(tag, this.target);
            }

            public ISyntax<TypeCheckTag> VisitBoolType(BoolType type) {
                throw TypeCheckingErrors.MemberUndefined(this.location, type, this.memberName);
            }

            public ISyntax<TypeCheckTag> VisitIntType(IntType type) {
                throw TypeCheckingErrors.MemberUndefined(this.location, type, this.memberName);
            }

            public ISyntax<TypeCheckTag> VisitNamedType(NamedType type) {
                if (!this.scope.FindTypeInfo(type.Path).TryGetValue(out var info)) {
                    throw new Exception("This isn't supposed to happen");
                }

                return info.Match(
                    varInfo => throw new InvalidOperationException(),
                    funcInfo => throw TypeCheckingErrors.MemberUndefined(this.location, type, this.memberName),
                    compositeInfo => {
                        var mem = compositeInfo.Signature.Members.FirstOrDefault(x => x.Name == this.memberName);

                        if (mem == null) {
                            throw TypeCheckingErrors.MemberUndefined(this.location, type, this.memberName);
                        }

                        var copiability = mem.Type.Accept(new TypeCopiabilityVisitor(this.scope));
                        ImmutableHashSet<VariableCapture> captured;

                        if (copiability == TypeCopiability.Unconditional) {
                            captured = ImmutableHashSet<VariableCapture>.Empty;
                        }
                        else if (copiability == TypeCopiability.Conditional) {
                            captured = this.target.Tag.CapturedVariables;
                        }
                        else {
                            throw TypeCheckingErrors.TypeNotCopiable(this.location, mem.Type);
                        }

                        var tag = new TypeCheckTag(mem.Type, captured);

                        return new CompositeMemberAccessSyntax(tag, this.target, this.memberName, compositeInfo);
                    });
            }

            public ISyntax<TypeCheckTag> VisitVariableType(VariableType type) {
                throw TypeCheckingErrors.MemberUndefined(this.location, type, this.memberName);
            }

            public ISyntax<TypeCheckTag> VisitVoidType(VoidType type) {
                throw TypeCheckingErrors.MemberUndefined(this.location, type, this.memberName);
            }
        }
    }
}