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
        public abstract PassingSemantics GetSemantics(ITypedFrame types);

        public abstract UnificationKind TestUnification(HelixType other, TypeFrame types);

        public abstract ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax,
                                           UnificationKind unificationKind,
                                           TypeFrame types);

        public virtual HelixType ToMutableType() {
            return this;
        }

        public virtual ISyntaxTree ToSyntax(TokenLocation loc) {
            return new TypeSyntaxWrapper(loc, this);
        }

        public virtual IEnumerable<HelixType> GetContainedTypes(TypeFrame frame) {
            yield return this;
        }

        public bool CanConvertTo(HelixType other, TypeFrame types) {
            return this.TestUnification(other, types).IsSubsetOf(UnificationKind.Convert);
        }

        public ISyntaxTree ConvertTo(HelixType other, ISyntaxTree syntax, TypeFrame types) {
            return this.UnifyTo(other, syntax, UnificationKind.Convert, types);
        }

        public bool CanConvertFrom(HelixType other, TypeFrame types) {
            return this.CanConvertTo(other, types) || other.CanConvertTo(this, types);
        }

        public bool IsValueType(ITypedFrame types) {
            return this.GetSemantics(types) == PassingSemantics.ValueType;
        }

        public HelixType ConvertFrom(HelixType other, TypeFrame types) {
            if (this.CanConvertTo(other, types)) {
                return other;
            }
            else if (other.CanConvertTo(this, types)) {
                return this;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public bool HasDefaultValue(TypeFrame types) {
            return PrimitiveType.Void.CanConvertTo(this, types);
        }

        private class TypeSyntaxWrapper : ISyntaxTree {
            private readonly HelixType type;

            public TokenLocation Location { get; }

            public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

            public bool IsPure => true;

            public TypeSyntaxWrapper(TokenLocation loc, HelixType type) {
                this.Location = loc;
                this.type = type;
            }

            public Option<HelixType> AsType(TypeFrame types) => this.type;

            public ISyntaxTree CheckTypes(TypeFrame types) {
                throw new InvalidOperationException();
            }

            public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
                throw new InvalidOperationException();
            }

            public void AnalyzeFlow(FlowFrame flow) {
                throw new InvalidOperationException();
            }
        }
    }
}