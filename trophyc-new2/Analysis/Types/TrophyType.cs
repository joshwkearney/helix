using Trophy.Parsing;

namespace Trophy.Analysis.Types {
    public abstract record TrophyType {
        public virtual bool CanUnifyWith(TrophyType other) => this == other;

        public virtual ISyntax UnifyTo(TrophyType other, ISyntax syntax) {
            if (this == other) {
                return syntax;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public virtual bool CanUnifyFrom(TrophyType other) {
            return this.CanUnifyWith(other) || other.CanUnifyWith(this);
        }

        public virtual TrophyType UnifyFrom(TrophyType other) {
            if (this.CanUnifyWith(other)) {
                return other;
            }
            else if (other.CanUnifyWith(this)) {
                return this;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public virtual Option<PointerType> AsPointerType() => new();

        public virtual Option<NamedType> AsNamedType() => new();
    }
}