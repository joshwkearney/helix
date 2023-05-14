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

        public abstract UnificationKind TestUnification(HelixType other, EvalFrame types);

        public abstract ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax,
                                           UnificationKind unificationKind,
                                           EvalFrame types);

        public virtual HelixType ToMutableType() {
            return this;
        }

        public virtual ISyntaxTree ToSyntax(TokenLocation loc) {
            return new TypeSyntaxWrapper(loc, this);
        }

        public virtual IEnumerable<HelixType> GetContainedTypes(EvalFrame frame) {
            yield return this;
        }

        public bool CanConvertTo(HelixType other, EvalFrame types) {
            return this.TestUnification(other, types).IsSubsetOf(UnificationKind.Convert);
        }

        public ISyntaxTree ConvertTo(HelixType other, ISyntaxTree syntax, EvalFrame types) {
            return this.UnifyTo(other, syntax, UnificationKind.Convert, types);
        }

        public bool CanConvertFrom(HelixType other, EvalFrame types) {
            return this.CanConvertTo(other, types) || other.CanConvertTo(this, types);
        }

        public bool IsValueType(ITypedFrame types) {
            return this.GetSemantics(types) == PassingSemantics.ValueType;
        }

        public HelixType ConvertFrom(HelixType other, EvalFrame types) {
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

        public bool HasDefaultValue(EvalFrame types) {
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

            public Option<HelixType> AsType(EvalFrame types) => this.type;

            public ISyntaxTree CheckTypes(EvalFrame types) {
                throw new InvalidOperationException();
            }

            public ICSyntax GenerateCode(EvalFrame types, ICStatementWriter writer) {
                throw new InvalidOperationException();
            }

            public void AnalyzeFlow(FlowFrame flow) {
                throw new InvalidOperationException();
            }
        }
    }
}