using Trophy.Generation;
using Trophy.Generation.Syntax;
using Trophy.Parsing;

namespace Trophy.Analysis.Types {
    public abstract record TrophyType {
        public virtual TrophyType ToMutableType() => this;

        public virtual ISyntaxTree ToSyntax(TokenLocation loc) {
            return new TypeSyntaxWrapper(loc, this);
        }

        public virtual IEnumerable<TrophyType> GetContainedValueTypes(SyntaxFrame types) {
            yield return this;
        }

        public virtual bool CanUnifyTo(TrophyType other, SyntaxFrame types, bool isCast) => this == other;

        public virtual ISyntaxTree UnifyTo(TrophyType other, ISyntaxTree syntax, bool isCast, SyntaxFrame types) {
            if (this == other) {
                return syntax;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public virtual bool CanUnifyFrom(TrophyType other, SyntaxFrame types) {
            return this.CanUnifyTo(other, types, false) || other.CanUnifyTo(this, types, false);
        }

        public virtual TrophyType UnifyFrom(TrophyType other, SyntaxFrame types) {
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

        public bool HasDefaultValue(SyntaxFrame types) {
            return PrimitiveType.Void.CanUnifyTo(this, types, false);
        }

        private class TypeSyntaxWrapper : ISyntaxTree {
            private readonly TrophyType type;

            public TokenLocation Location { get; }

            public TypeSyntaxWrapper(TokenLocation loc, TrophyType type) {
                this.Location = loc;
                this.type = type;
            }

            public Option<TrophyType> AsType(SyntaxFrame types) => this.type;

            public ISyntaxTree CheckTypes(SyntaxFrame types) {
                throw new InvalidOperationException();
            }

            public ICSyntax GenerateCode(ICStatementWriter writer) {
                throw new InvalidOperationException();
            }
        }
    }
}