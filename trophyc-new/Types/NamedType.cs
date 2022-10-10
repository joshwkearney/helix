using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Trophy.Parsing {
    public class NamedType : ITrophyType {
        public string Path { get; }

        public NamedType(string path) {
            this.Path = path;
        }

        public bool AsNamedType(out NamedType type) {
            type = this;
            return true;
        }

        public bool HasDefaultValue(NameTable types) {
            if (types.FunctionSignatures.TryGetValue(this.Path, out _)) {
                return true;
            }
            else if (types.StructSignatures.TryGetValue(this.Path, out var structSig)) {
                return structSig.Members.All(x => x.Type.HasDefaultValue(types));
            }
            else if (types.UnionSignatures.TryGetValue(this.Path, out var unionSig)) {
                return unionSig.Members.First().Type.HasDefaultValue(types);
            }
            else {
                throw new NotImplementedException();
            }
        }

        public override int GetHashCode() {
            return this.Path.GetHashCode();
        }

        public override string ToString() {
            return this.Path.Split('.').Last();
        }

        public override bool Equals(object other) {
            return this.Equals(other as ITrophyType);
        }

        public bool Equals([AllowNull] ITrophyType other) {
            return other is NamedType type && this.Path == type.Path;
        }
    }
}