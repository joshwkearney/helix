using Helix.Common;
using Helix.Common.Hmm;
using Helix.Common.Tokens;
using Helix.Common.Types;

namespace Helix.MiddleEnd.TypeChecking {
    internal enum UnificationKind {
        Pun, Convert, Cast
    }

    internal delegate string Unifier(string name, TokenLocation location);

    internal interface IUnificationFactory {
        public Option<Unifier> CreateUnifier(IHelixType fromType, IHelixType toType, UnificationKind kind, AnalysisContext context);
    }

    internal class VoidUnificationFactory : IUnificationFactory {
        public static VoidUnificationFactory Instance { get; } = new();

        public Option<Unifier> CreateUnifier(IHelixType fromType, IHelixType toType, UnificationKind kind, AnalysisContext context) {
            Assert.IsTrue(fromType == VoidType.Instance);

            if (toType == WordType.Instance) {
                return Option.Some<Unifier>((_, _) => "0");
            }
            else if (toType == BoolType.Instance) {
                return Option.Some<Unifier>((_, _) => "false");
            }

            if (toType.GetArraySignature(context).TryGetValue(out var arraySig)) {
                return Option.Some<Unifier>((value, loc) => {
                    var name = context.Names.GetConvertName();

                    var line = new HmmNewSyntax() {
                        Location = loc,
                        Result = name,
                        Type = toType
                    };

                    return line.Accept(context.TypeChecker).ResultName;
                });
            }

            return Option.None;
        }
    }

    internal class SingularWordUnificationFactory : IUnificationFactory {
        public static SingularWordUnificationFactory Instance { get; } = new();

        public Option<Unifier> CreateUnifier(IHelixType fromType, IHelixType toType, UnificationKind kind, AnalysisContext context) {
            if (fromType is not SingularWordType sing) {
                throw Assert.Fail();
            }

            if (toType == WordType.Instance) {
                return Option.Some<Unifier>((_, _) => sing.ToString());
            }
            else if (toType == BoolType.Instance && sing.Value == 1) {
                return Option.Some<Unifier>((_, _) => "true");
            }
            else if (toType == BoolType.Instance && sing.Value == 0) {
                return Option.Some<Unifier>((_, _) => "false");
            }

            return Option.None;
        }
    }
    internal class SingularBoolUnificationFactory : IUnificationFactory {
        public static SingularBoolUnificationFactory Instance { get; } = new();

        public Option<Unifier> CreateUnifier(IHelixType fromType, IHelixType toType, UnificationKind kind, AnalysisContext context) {
            if (fromType is not SingularBoolType sing) {
                throw Assert.Fail();
            }

            if (toType == BoolType.Instance) {
                return Option.Some<Unifier>((_, _) => sing.ToString());
            }
            else if (toType == WordType.Instance) {
                return Option.Some<Unifier>((_, _) => sing.Value ? "1" : "0");
            }

            return Option.None;
        }
    }

    internal class ToUnionUnificationFactory : IUnificationFactory {
        public static ToUnionUnificationFactory Instance { get; } = new();

        public Option<Unifier> CreateUnifier(IHelixType fromType, IHelixType toType, UnificationKind kind, AnalysisContext context) {
            if (!toType.GetUnionSignature(context).TryGetValue(out var unionType)) {
                throw Assert.Fail();
            }

            if (context.Unifier.CanUnify(fromType, unionType.Members[0].Type, kind)) {
                return Option.Some<Unifier>((value, loc) => {
                    var name = context.Names.GetConvertName();

                    var syntax = new HmmNewSyntax() {
                        Location = loc,
                        Assignments = [
                            new HmmNewFieldAssignment() {
                                Field = unionType.Members[0].Name,
                                Value = value
                            }
                        ],
                        Type = toType,
                        Result = name
                    };

                    return syntax.Accept(context.TypeChecker).ResultName;
                });
            }

            return Option.None;
        }
    }
}
