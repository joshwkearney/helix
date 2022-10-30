using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Analysis.Types {
    public abstract record HelixType {
        public virtual HelixType ToMutableType() => this;

        public virtual ISyntaxTree ToSyntax(TokenLocation loc) {
            return new TypeSyntaxWrapper(loc, this);
        }

        public abstract bool IsRemote(EvalFrame types);

        public virtual IEnumerable<HelixType> GetContainedTypes(EvalFrame frame) {
            yield return this;
        }

        public virtual bool CanUnifyTo(HelixType other, EvalFrame types, bool isCast) => this == other;

        public virtual ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax, bool isCast, EvalFrame types) {
            if (this == other) {
                return syntax;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public virtual bool CanUnifyFrom(HelixType other, EvalFrame types) {
            return this.CanUnifyTo(other, types, false) || other.CanUnifyTo(this, types, false);
        }

        public virtual HelixType UnifyFrom(HelixType other, EvalFrame types) {
            if (this.CanUnifyTo(other, types, false)) {
                return other;
            }
            else if (other.CanUnifyTo(this, types, false)) {
                return this;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public bool HasDefaultValue(EvalFrame types) {
            return PrimitiveType.Void.CanUnifyTo(this, types, false);
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
        }
    }
}