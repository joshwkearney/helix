using Helix.Common;
using Helix.Common.Tokens;
using Helix.Common.Types;
using Helix.MiddleEnd.TypeChecking;
using System;

namespace Helix.MiddleEnd.Unification {
    internal class TypeUnifier {
        private readonly TypeCheckingContext context;

        public TypeUnifier(TypeCheckingContext context) {
            this.context = context;
        }

        //public bool CanCast(IHelixType fromType, IHelixType toType) => GetUnifier(fromType, toType, UnificationKind.Cast).HasValue;

        public bool CanConvert(IHelixType fromType, IHelixType toType) => GetUnifier(fromType, toType, UnificationKind.Convert).HasValue;

        //public bool CanPun(IHelixType fromType, IHelixType toType) => GetUnifier(fromType, toType, UnificationKind.Pun).HasValue;

        public bool CanUnify(IHelixType fromType, IHelixType toType, UnificationKind kind) => GetUnifier(fromType, toType, kind).HasValue;

        //public string Cast(string value, IHelixType toType, TokenLocation loc) {
        //    var fromType = context.Types.GetLocalType(value);

        //    if (!GetUnifier(fromType, toType, UnificationKind.Cast).TryGetValue(out var unifier)) {
        //        throw new InvalidOperationException();
        //    }

        //    return unifier.Invoke(value, loc);
        //}

        public string Convert(string value, IHelixType toType, TokenLocation loc) {
            var fromType = context.Types.GetType(value);

            if (!GetUnifier(fromType, toType, UnificationKind.Convert).TryGetValue(out var unifier)) {
                throw TypeCheckException.TypeConversionFailed(loc, fromType, toType);
            }

            var result = unifier.Invoke(value, loc);
            var resultType = this.context.Types.GetType(result);

            Assert.IsTrue(resultType.GetSupertype() == toType);

            return result;
        }

        //public string Pun(string value, IHelixType toType, TokenLocation loc) {
        //    var fromType = context.Types.GetLocalType(value);

        //    if (!GetUnifier(fromType, toType, UnificationKind.Pun).TryGetValue(out var unifier)) {
        //        throw new InvalidOperationException();
        //    }

        //    return unifier.Invoke(value, loc);
        //}

        //public Option<IHelixType> TryUnifyWithCast(IHelixType fromType, IHelixType toType) => GetUnifyingType(fromType, toType, UnificationKind.Cast);

        public Option<IHelixType> TryUnifyWithConvert(IHelixType fromType, IHelixType toType) {
            var result = GetUnifyingType(fromType, toType, UnificationKind.Convert);

            if (result.TryGetValue(out var type)) {
                Assert.IsTrue(fromType == type || fromType.GetSupertype() == type);
                Assert.IsTrue(toType == type || toType.GetSupertype() == type);
            }

            return result;
        }

        //public Option<IHelixType> TryUnifyWithPun(IHelixType fromType, IHelixType toType) => GetUnifyingType(fromType, toType, UnificationKind.Pun);

        //public IHelixType UnifyWithCast(IHelixType type1, IHelixType type2, TokenLocation loc) {
        //    if (!this.TryUnifyWithCast(type1, type2).TryGetValue(out var type)) {
        //        throw TypeCheckException.TypeUnificationFailed(loc, type1, type2);
        //    }

        //    return type;
        //}

        public IHelixType UnifyWithConvert(IHelixType type1, IHelixType type2, TokenLocation loc) {
            if (!this.TryUnifyWithConvert(type1, type2).TryGetValue(out var type)) {
                throw TypeCheckException.TypeUnificationFailed(loc, type1, type2);
            }

            Assert.IsTrue(type1 == type || type1.GetSupertype() == type);
            Assert.IsTrue(type2 == type || type2.GetSupertype() == type);

            return type;
        }

        //public IHelixType UnifyWithPun(IHelixType type1, IHelixType type2, TokenLocation loc) {
        //    if (!this.TryUnifyWithPun(type1, type2).TryGetValue(out var type)) {
        //        throw TypeCheckException.TypeUnificationFailed(loc, type1, type2);
        //    }

        //    return type;
        //}

        private Option<IHelixType> GetUnifyingType(IHelixType type1, IHelixType type2, UnificationKind kind) {
            if (GetUnifier(type1, type2, kind).TryGetValue(out var u)) {
                return type2;
            }
            else if (GetUnifier(type2, type1, kind).TryGetValue(out u)) {
                return type1;
            }

            var sig1 = type1.GetSupertype();
            var sig2 = type2.GetSupertype();

            if (GetUnifier(type1, sig2, kind).TryGetValue(out u)) {
                return sig2;
            }
            else if (GetUnifier(type2, sig1, kind).TryGetValue(out u)) {
                return sig1;
            }

            return Option.None;
        }

        private Option<Unifier> GetUnifier(IHelixType fromType, IHelixType toType, UnificationKind kind) {
            if (fromType == toType) {
                return Option.Some<Unifier>((value, _) => value);
            }

            var kinds = new[] { kind };

            if (kind == UnificationKind.Convert) {
                kinds = [UnificationKind.Convert, UnificationKind.Pun];
            }
            else if (kind == UnificationKind.Cast) {
                kinds = [UnificationKind.Cast, UnificationKind.Convert, UnificationKind.Pun];
            }

            return kinds
                .SelectMany(kind => GetUnifiers(fromType, toType).SelectMany(x => x.CreateUnifier(fromType, toType, kind, context).ToEnumerable()))
                .FirstOrNone();
        }

        private IEnumerable<IUnificationFactory> GetUnifiers(IHelixType fromType, IHelixType toType) {
            if (fromType.IsVoid) {
                yield return VoidUnificationFactory.Instance;
            }
            else if (fromType is SingularWordType) {
                yield return SingularWordUnificationFactory.Instance;
            }
            else if (fromType is SingularBoolType) {
                yield return SingularBoolUnificationFactory.Instance;
            }

            if (toType.GetUnionSignature(this.context).HasValue) {
                yield return ToUnionUnificationFactory.Instance;
            }
        }
    }
}
