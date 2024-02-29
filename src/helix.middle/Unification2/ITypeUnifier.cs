using helix.common;
using Helix.Analysis.Types;
using Helix.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.Unification {
    internal class TypeUnifier {
        private readonly TypeCheckingContext context;

        public TypeUnifier(TypeCheckingContext context) {
            this.context = context;
        }

        public bool CanCast(IHelixType fromType, IHelixType toType) => this.GetUnifier(fromType, toType, UnificationKind.Cast).HasValue;

        public bool CanConvert(IHelixType fromType, IHelixType toType) => this.GetUnifier(fromType, toType, UnificationKind.Convert).HasValue;

        public bool CanPun(IHelixType fromType, IHelixType toType) => this.GetUnifier(fromType, toType, UnificationKind.Pun).HasValue;

        public string Cast(string value, IHelixType toType, TokenLocation loc) {
            Assert.IsTrue(this.context.Types.ContainsType(value));

            var fromType = this.context.Types[value];
            if (!this.GetUnifier(fromType, toType, UnificationKind.Cast).TryGetValue(out var unifier)) {
                throw new InvalidOperationException();
            }

            return unifier.Invoke(value, loc);
        }

        public string Convert(string value, IHelixType toType, TokenLocation loc) {
            Assert.IsTrue(this.context.Types.ContainsType(value));

            var fromType = this.context.Types[value];
            if (!this.GetUnifier(fromType, toType, UnificationKind.Convert).TryGetValue(out var unifier)) {
                throw new InvalidOperationException();
            }

            return unifier.Invoke(value, loc);
        }

        public string Pun(string value, IHelixType toType, TokenLocation loc) {
            Assert.IsTrue(this.context.Types.ContainsType(value));

            var fromType = this.context.Types[value];
            if (!this.GetUnifier(fromType, toType, UnificationKind.Pun).TryGetValue(out var unifier)) {
                throw new InvalidOperationException();
            }

            return unifier.Invoke(value, loc);
        }

        public Option<IHelixType> UnifyWithCast(IHelixType fromType, IHelixType toType) => this.GetUnifyingType(fromType, toType, UnificationKind.Cast);

        public Option<IHelixType> UnifyWithConvert(IHelixType fromType, IHelixType toType) => this.GetUnifyingType(fromType, toType, UnificationKind.Convert);

        public Option<IHelixType> UnifyWithPun(IHelixType fromType, IHelixType toType) => this.GetUnifyingType(fromType, toType, UnificationKind.Pun);

        private Option<IHelixType> GetUnifyingType(IHelixType type1, IHelixType type2, UnificationKind kind) {
            if (this.GetUnifier(type1, type2, kind).TryGetValue(out var u)) {
                return type2;
            }
            else if (this.GetUnifier(type2, type1, kind).TryGetValue(out u)) {
                return type1;
            }

            var sig1 = type1.GetSupertype();
            var sig2 = type2.GetSupertype();

            if (this.GetUnifier(type1, sig2, kind).TryGetValue(out u)) {
                return sig2;
            }
            else if (this.GetUnifier(type2, sig1, kind).TryGetValue(out u)) {
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
                .SelectMany(kind => GetUnifiers(fromType, toType).SelectMany(x => x.CreateUnifier(fromType, toType, kind, this.context).ToEnumerable()))
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
        }
    }
}
