using Trophy.Parsing;

namespace Trophy.Analysis.Types {
    public abstract record TrophyType {
        public virtual bool CanUnifyTo(TrophyType other) => this == other;

        public virtual ISyntax UnifyTo(TrophyType other, ISyntax syntax) {
            if (this == other) {
                return syntax;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public virtual bool CanUnifyFrom(TrophyType other) {
            return this.CanUnifyTo(other) || other.CanUnifyTo(this);
        }

        public virtual TrophyType UnifyFrom(TrophyType other) {
            if (this.CanUnifyTo(other)) {
                return other;
            }
            else if (other.CanUnifyTo(this)) {
                return this;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public virtual TrophyType ToMutableType() => this;

        public virtual IEnumerable<TrophyType> GetContainedValueTypes(ITypesRecorder types) {
            yield return this;
        }
    }
}