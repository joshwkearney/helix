﻿using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Features.Arrays;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Structs {
    public class ValueMemberAccessTarget : IMemberAccessTarget {
        private readonly ISyntax<TypeCheckTag> value;
        private readonly IScope scope;
        private readonly TokenLocation loc;

        public ValueMemberAccessTarget(ISyntax<TypeCheckTag> value, TokenLocation loc, IScope scope) {
            this.value = value;
            this.scope = scope;
            this.loc = loc;
        }

        public IMemberAccessTarget AccessMember(string name) {
            var value = this.value.Tag.ReturnType.Accept(new MemberAccessVisitor(name, this.loc, this.value, this.scope));

            return new ValueMemberAccessTarget(value, this.loc, this.scope);
        }

        public IMemberAccessTarget InvokeMember(string name, ImmutableList<ISyntax<TypeCheckTag>> arguments) {
            throw new NotImplementedException();
        }

        public ISyntax<TypeCheckTag> ToSyntax() {
            return this.value;
        }

        private class MemberAccessVisitor : ITypeVisitor<ISyntax<TypeCheckTag>> {
            private readonly string memberName;
            private readonly IScope scope;
            private readonly TokenLocation location;
            private readonly ISyntax<TypeCheckTag> target;

            public MemberAccessVisitor(string name, TokenLocation loc, ISyntax<TypeCheckTag> target, IScope scope) {
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
                    structInfo => {
                        var mem = structInfo.Signature.Members.FirstOrDefault(x => x.Name == this.memberName);

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

                        return new StructMemberAccessSyntax(tag, this.target, this.memberName);
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