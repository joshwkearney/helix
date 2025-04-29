using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.TypeChecking;

namespace Helix.Types {
    public enum UnificationKind {
        None,
        Pun,
        Convert,
        Cast
    }

    public enum PassingSemantics {
        ValueType, ContainsReferenceType, ReferenceType
    }

    public static class TypeExtensions {
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
    }

    public abstract record HelixType { 
        public abstract PassingSemantics GetSemantics(TypeFrame types);

        public abstract HelixType GetSignature(TypeFrame types);
        
        public virtual Option<ITypedExpression> ToSyntax(TokenLocation loc, TypeFrame types) {
            return Option.None;
        }

        public virtual IEnumerable<HelixType> GetAccessibleTypes(TypeFrame frame) {
            yield return this;
        }

        public bool IsValueType(TypeFrame types) {
            return this.GetSemantics(types) == PassingSemantics.ValueType;
        }

        public virtual Option<PointerType> AsVariable(TypeFrame types) => Option.None;

        public virtual Option<FunctionType> AsFunction(TypeFrame types) => Option.None;

        public virtual Option<StructType> AsStruct(TypeFrame types) => Option.None;

        public virtual Option<UnionType> AsUnion(TypeFrame types) => Option.None;

        public virtual Option<ArrayType> AsArray(TypeFrame types) => Option.None;

        public virtual bool IsBool(TypeFrame types) => false;

        public virtual bool IsWord(TypeFrame types) => false;
    }
}