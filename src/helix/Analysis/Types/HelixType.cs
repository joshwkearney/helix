using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Analysis.Types {
    public abstract record HelixType {
        public virtual HelixType ToMutableType() => this;

        public virtual ISyntaxTree ToSyntax(TokenLocation loc) {
            return new TypeSyntaxWrapper(loc, this);
        }

        public virtual IEnumerable<HelixType> GetContainedValueTypes(SyntaxFrame types) {
            yield return this;
        }

        public virtual bool CanUnifyTo(HelixType other, SyntaxFrame types, bool isCast) => this == other;

        public virtual ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax, bool isCast, SyntaxFrame types) {
            if (this == other) {
                return syntax;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public virtual bool CanUnifyFrom(HelixType other, SyntaxFrame types) {
            return this.CanUnifyTo(other, types, false) || other.CanUnifyTo(this, types, false);
        }

        public virtual HelixType UnifyFrom(HelixType other, SyntaxFrame types) {
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
            private readonly HelixType type;

            public TokenLocation Location { get; }

            public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

            public bool IsPure => true;

            public TypeSyntaxWrapper(TokenLocation loc, HelixType type) {
                this.Location = loc;
                this.type = type;
            }

            public Option<HelixType> AsType(SyntaxFrame types) => this.type;

            public ISyntaxTree CheckTypes(SyntaxFrame types) {
                throw new InvalidOperationException();
            }

            public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
                throw new InvalidOperationException();
            }
        }
    }
}