using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Features.Arrays;
using Attempt17.Features.Containers;
using Attempt17.Features.Primitives;
using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public class TypeVoidValueVisitor : ITypeVisitor<IOption<ISyntax<TypeCheckTag>>> {
        private readonly ITypeCheckScope scope;

        public TypeVoidValueVisitor(ITypeCheckScope scope) {
            this.scope = scope;
        }

        public IOption<ISyntax<TypeCheckTag>> VisitArrayType(ArrayType arrType) {
            // Make sure the elements have a default value
            if (arrType.ElementType.Accept(this).Any()) {
                return Option.Some(
                    new ArrayLiteralSyntax<TypeCheckTag>(
                        new TypeCheckTag(arrType),
                        ImmutableList<ISyntax<TypeCheckTag>>.Empty));
            }

            return Option.None<ISyntax<TypeCheckTag>>();
        }

        public IOption<ISyntax<TypeCheckTag>> VisitBoolType(BoolType type) {
            return Option.Some(
                    new BoolLiteralSyntax<TypeCheckTag>(
                        new TypeCheckTag(type),
                        false));
        }

        public IOption<ISyntax<TypeCheckTag>> VisitIntType(IntType type) {
            return Option.Some(
                    new IntLiteralSyntax<TypeCheckTag>(
                        new TypeCheckTag(type),
                        0L));
        }

        public IOption<ISyntax<TypeCheckTag>> VisitNamedType(NamedType type) {
            if (!this.scope.FindTypeInfo(type.Path).TryGetValue(out var info)) {
                throw new Exception("This should never happen");
            }

            return info.Match(
                varInfo => throw new InvalidOperationException(),
                funcInfo => Option.None<ISyntax<TypeCheckTag>>(),
                structInfo => {
                    var allDefault = structInfo.Signature
                        .Members
                        .Select(x => new {
                            x.Name,
                            Value = x.Type.Accept(this)
                        })
                        .ToArray();

                    if (!allDefault.All(x => x.Value.Any())) {
                        return Option.None<ISyntax<TypeCheckTag>>();
                    }

                    var insts = allDefault
                        .Select(x => new MemberInstantiation<TypeCheckTag>(x.Name, x.Value.GetValue()))
                        .ToImmutableList();

                    var tag = new TypeCheckTag(structInfo.StructType);

                    return Option.Some(new NewStructSyntax<TypeCheckTag>(tag, structInfo, insts));
                });
        }

        public IOption<ISyntax<TypeCheckTag>> VisitVariableType(VariableType type) {
            return Option.None<ISyntax<TypeCheckTag>>();
        }

        public IOption<ISyntax<TypeCheckTag>> VisitVoidType(VoidType type) {
            return Option.Some(
                    new VoidLiteralSyntax<TypeCheckTag>(
                        new TypeCheckTag(type)));
        }
    }
}