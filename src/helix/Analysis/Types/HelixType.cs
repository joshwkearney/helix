using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Analysis.Types {
    public enum UnificationKind {
        None,
        Pun,
        Convert,
        Cast
    }

    public enum PassingSemantics {
        ValueType, ContainsReferenceType, ReferenceType
    }

    public static partial class TypeExtensions {
        public static bool IsSubsetOf(this UnificationKind unify, UnificationKind other) {
            if (other == UnificationKind.Cast) {
                return unify == UnificationKind.Cast
                    || unify == UnificationKind.Convert
                    || unify == UnificationKind.Pun;
            }
            else if (other == UnificationKind.Convert) {
                return unify == UnificationKind.Convert
                    || unify == UnificationKind.Pun;
            }
            else if (other == UnificationKind.Pun) {
                return unify == UnificationKind.Pun;
            }
            else {
                return false;
            }
        }

        public static bool IsValueType(this PassingSemantics passing) {
            return passing == PassingSemantics.ValueType;
        }
    }

    public abstract record HelixType { 
        public abstract PassingSemantics GetSemantics(TypeFrame types);

        public abstract HelixType GetMutationSupertype(TypeFrame types);

        public abstract HelixType GetSignatureSupertype(TypeFrame types);

        public virtual Option<ISyntaxTree> ToSyntax(TokenLocation loc) {
            return Option.None;
        }

        public virtual IEnumerable<HelixType> GetContainedTypes(TypeFrame frame) {
            yield return this;
        }

        public bool IsValueType(TypeFrame types) {
            return this.GetSemantics(types) == PassingSemantics.ValueType;
        }
    }
}