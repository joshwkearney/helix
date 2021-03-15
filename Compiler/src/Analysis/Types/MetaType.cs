using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Trophy.Analysis.Types {
    public class MetaType : TrophyType {
        public IdentifierPath Path { get; }

        public IReadOnlyList<string> Arguments { get; }

        public MetaType(IdentifierPath path, IReadOnlyList<string> args) {
            this.Path = path;
            this.Arguments = args;
        }

        public override bool Equals(object other) {
            return other is MetaType meta
                && this.Path == meta.Path
                && this.Arguments.SequenceEqual(meta.Arguments);
        }

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Unconditional;
        }

        public override int GetHashCode() {
            return this.Path.GetHashCode() 
                + 29 * this.Arguments.Aggregate(11, (x, y) => x + 7 * y.GetHashCode());
        }

        public override bool HasDefaultValue(ITypeRecorder types) {
            return true;
        }

        public override string ToString() {
            return this.Path.Segments.Last()
                + "["
                + string.Join(",", this.Arguments)
                + "]";
        }
    }
}