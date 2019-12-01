using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JoshuaKearney.Attempt15.Compiling;

namespace JoshuaKearney.Attempt15.Types {
    public class TupleType : ITrophyType, IEquatable<TupleType> {
        public bool IsReferenceCounted {
            get {
                foreach (var mem in this.Members) {
                    if (mem.Type.IsReferenceCounted) {
                        return true;
                    }
                }

                return false;
            }
        }

        public TrophyTypeKind Kind => TrophyTypeKind.Tuple;

        public IReadOnlyList<IdentifierInfo> Members { get; }

        public TupleType(IEnumerable<IdentifierInfo> members) {
            this.Members = members.ToArray();
        }

        public string GenerateName(CodeGenerateEventArgs args) {
            return args.TupleGenerator.GetTupleTypeName(this, args);
        }

        public override bool Equals(object obj) {
            if (obj is TupleType tuple) {
                return this.Equals(tuple);
            }
            else {
                return false;
            }
        }

        public override int GetHashCode() {
            return this.Members.Aggregate(this.Kind.GetHashCode(), (x, y) => x + y.GetHashCode());
        }

        public bool Equals(TupleType other) {
            return this.Members.SequenceEqual(other.Members);
        }
    }
}