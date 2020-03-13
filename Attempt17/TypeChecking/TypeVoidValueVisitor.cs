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

            return info.Accept(new IdentifierTargetVisitor<IOption<ISyntax<TypeCheckTag>>>() {
                HandleFunction = _ => Option.None<ISyntax<TypeCheckTag>>(),
                HandleComposite = compositeInfo => {
                    if (compositeInfo.Kind == CompositeKind.Class) {
                        return Option.None<ISyntax<TypeCheckTag>>();
                    }
                    else if (compositeInfo.Kind == CompositeKind.Struct) {
                        var allDefault = compositeInfo.Signature
                            .Members
                            .Select(x => new {
                                x.Name,
                                Value = x.Type.Accept(this)
                            })
                            .ToArray();

                        // Every member of a struct must have a default value
                        if (!allDefault.All(x => x.Value.Any())) {
                            return Option.None<ISyntax<TypeCheckTag>>();
                        }

                        var insts = allDefault
                            .Select(x => new MemberInstantiation<TypeCheckTag>(x.Name, x.Value.GetValue()))
                            .ToImmutableList();

                        var tag = new TypeCheckTag(compositeInfo.Type);

                        return Option.Some(new NewCompositeSyntax<TypeCheckTag>(tag, compositeInfo, insts));
                    }
                    else if (compositeInfo.Kind == CompositeKind.Union) {
                        var mem = compositeInfo.Signature.Members.FirstOrDefault();
                        var tag = new TypeCheckTag(compositeInfo.Type);
                        var insts = ImmutableList<MemberInstantiation<TypeCheckTag>>.Empty;

                        // If there are no members, then there is a default value
                        if (mem == null) {
                            return Option.Some(new NewCompositeSyntax<TypeCheckTag>(tag, compositeInfo, insts));
                        }

                        // The first member must have a default value
                        if (!mem.Type.Accept(this).TryGetValue(out var voidValue)) {
                            return Option.None<ISyntax<TypeCheckTag>>();
                        }

                        insts = insts.Add(new MemberInstantiation<TypeCheckTag>(mem.Name, voidValue));

                        return Option.Some(new NewCompositeSyntax<TypeCheckTag>(tag, compositeInfo, insts));
                    }
                    else {
                        throw new Exception();
                    }
                }
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