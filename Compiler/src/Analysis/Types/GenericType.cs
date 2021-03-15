using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Trophy.Analysis.Types {
    public class GenericType : ITrophyType {
        public IdentifierPath Path { get; }

        public IReadOnlyList<string> Arguments { get; }

        public GenericType(IdentifierPath path, IReadOnlyList<string> args) {
            this.Path = path;
            this.Arguments = args;
        }

        public TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Unconditional;
        }

        public override int GetHashCode() {
            return this.Path.GetHashCode() 
                + 29 * this.Arguments.Aggregate(11, (x, y) => x + 7 * y.GetHashCode());
        }

        public bool HasDefaultValue(ITypeRecorder types) {
            return true;
        }

        public override string ToString() {
            return this.Path.Segments.Last()
                + "["
                + string.Join(",", this.Arguments)
                + "]";
        }

        public override bool Equals(object other) {
            return this.Equals(other as ITrophyType);
        }

        public bool Equals(ITrophyType other) {
            return other is GenericType meta
                && this.Path == meta.Path
                && this.Arguments.SequenceEqual(meta.Arguments);
        }
    }
}