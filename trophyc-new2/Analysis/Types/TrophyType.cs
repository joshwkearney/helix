using Trophy.Generation;
using Trophy.Generation.Syntax;
using Trophy.Parsing;

namespace Trophy.Analysis.Types {
    public abstract record TrophyType {
        public virtual TrophyType ToMutableType() => this;

        public virtual ISyntax ToSyntax(TokenLocation loc) {
            return new TypeSyntaxWrapper(loc, this);
        }

        public virtual IEnumerable<TrophyType> GetContainedValueTypes(ITypesRecorder types) {
            yield return this;
        }

        public virtual bool CanUnifyTo(TrophyType other, ITypesRecorder types) => this == other;

        public virtual ISyntax UnifyTo(TrophyType other, ISyntax syntax, ITypesRecorder types) {
            if (this == other) {
                return syntax;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public virtual bool CanUnifyFrom(TrophyType other, ITypesRecorder types) {
            return this.CanUnifyTo(other, types) || other.CanUnifyTo(this, types);
        }

        public virtual TrophyType UnifyFrom(TrophyType other, ITypesRecorder types) {
            if (this.CanUnifyTo(other, types)) {
                return other;
            }
            else if (other.CanUnifyTo(this, types)) {
                return this;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public bool HasDefaultValue(ITypesRecorder types) {
            return PrimitiveType.Void.CanUnifyTo(this, types);
        }

        private class TypeSyntaxWrapper : ISyntax {
            private readonly TrophyType type;

            public TokenLocation Location { get; }

            public TypeSyntaxWrapper(TokenLocation loc, TrophyType type) {
                this.Location = loc;
                this.type = type;
            }

            public Option<TrophyType> TryInterpret(INamesRecorder names) => this.type;

            public ISyntax CheckTypes(ITypesRecorder types) {
                throw new InvalidOperationException();
            }

            public ICSyntax GenerateCode(ICStatementWriter writer) {
                throw new InvalidOperationException();
            }
        }
    }
}