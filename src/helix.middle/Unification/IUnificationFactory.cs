using Helix.Common;
using Helix.Common.Hmm;
using Helix.Common.Tokens;
using Helix.Common.Types;
using Helix.MiddleEnd.TypeChecking;

namespace Helix.MiddleEnd.Unification {
    internal enum UnificationKind {
        Pun, Convert, Cast
    }

    internal delegate string Unifier(string name, TokenLocation location);

    internal interface IUnificationFactory {
        public Option<Unifier> CreateUnifier(IHelixType fromType, IHelixType toType, UnificationKind kind, TypeCheckingContext context);
    }

    internal class VoidUnificationFactory : IUnificationFactory {
        public static VoidUnificationFactory Instance { get; } = new();

        public Option<Unifier> CreateUnifier(IHelixType fromType, IHelixType toType, UnificationKind kind, TypeCheckingContext context) {
            Assert.IsTrue(fromType == VoidType.Instance);

            if (toType == WordType.Instance) {
                return Option.Some<Unifier>((_, _) => "0");
            }
            else if (toType == BoolType.Instance) {
                return Option.Some<Unifier>((_, _) => "false");
            }
            else if (toType.GetUnionSignature(context).TryGetValue(out var unionType)) {
                if (unionType.Members[0].Type.HasVoidValue(context)) {
                    return Option.Some<Unifier>((value, loc) => {
                        var name = context.Names.GetConvertName();

                        var syntax = new HmmNewSyntax() {
                            Location = loc,
                            Assignments = [],
                            Type = toType,
                            Result = name
                        };

                        return syntax.Accept(context.TypeChecker);
                    });
                }
            }

            return Option.None;
        }
    }

    internal class SingularWordUnificationFactory : IUnificationFactory {
        public static SingularWordUnificationFactory Instance { get; } = new();

        public Option<Unifier> CreateUnifier(IHelixType fromType, IHelixType toType, UnificationKind kind, TypeCheckingContext context) {
            if (fromType is not SingularWordType sing) {
                Assert.IsTrue(fromType is SingularWordType);
                throw new InvalidOperationException();
            }

            if (toType == WordType.Instance) {
                return Option.Some<Unifier>((_, _) => sing.Value.ToString());
            }
            else if (toType == BoolType.Instance && sing.Value == 1) {
                return Option.Some<Unifier>((_, _) => "true");
            }
            else if (sing.Value == 0) {
                return VoidUnificationFactory.Instance.CreateUnifier(VoidType.Instance, toType, kind, context);
            }

            return Option.None;
        }
    }
    internal class SingularBoolUnificationFactory : IUnificationFactory {
        public static SingularWordUnificationFactory Instance { get; } = new();

        public Option<Unifier> CreateUnifier(IHelixType fromType, IHelixType toType, UnificationKind kind, TypeCheckingContext context) {
            if (fromType is not SingularBoolType sing) {
                Assert.IsTrue(fromType is SingularBoolType);
                throw new InvalidOperationException();
            }

            if (toType == BoolType.Instance) {
                return Option.Some<Unifier>((_, _) => sing.Value.ToString());
            }
            else if (toType == WordType.Instance) {
                return Option.Some<Unifier>((_, _) => sing.Value ? "1" : "0");
            }
            else if (!sing.Value) {
                return VoidUnificationFactory.Instance.CreateUnifier(VoidType.Instance, toType, kind, context);
            }

            return Option.None;
        }
    }
}
