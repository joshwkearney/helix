using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Trophy.Analysis.Types {
    public class NamedType : ITrophyType {
        public IdentifierPath SignaturePath { get; }

        public NamedType(IdentifierPath path) {
            this.SignaturePath = path;
        }

        public IOption<IdentifierPath> AsNamedType() {
            return Option.Some(this.SignaturePath);
        }

        public bool HasDefaultValue(ITypeRecorder types) {
            if (types.TryGetFunction(this.SignaturePath).Any()) {
                return true;
            }
            else if (types.TryGetStruct(this.SignaturePath).TryGetValue(out var structSig)) {
                return structSig.Members.All(x => x.MemberType.HasDefaultValue(types));
            }
            else if (types.TryGetUnion(this.SignaturePath).TryGetValue(out var unionSig)) {
                return unionSig.Members.First().MemberType.HasDefaultValue(types);
            }
            else {
                throw new NotImplementedException();
            }
        }

        public TypeCopiability GetCopiability(ITypeRecorder types) {
            if (types.TryGetFunction(this.SignaturePath).Any()) {
                return TypeCopiability.Unconditional;
            }
            else if (types.TryGetStruct(this.SignaturePath).TryGetValue(out var structSig)) {
                if (structSig.Members.All(x => x.MemberType.GetCopiability(types) == TypeCopiability.Unconditional)) {
                    return TypeCopiability.Unconditional;
                }
                else {
                    return TypeCopiability.Conditional;
                }
            }
            else if (types.TryGetUnion(this.SignaturePath).TryGetValue(out var unionSig)) {
                if (structSig.Members.All(x => x.MemberType.GetCopiability(types) == TypeCopiability.Unconditional)) {
                    return TypeCopiability.Unconditional;
                }
                else {
                    return TypeCopiability.Conditional;
                }
            }
            else {
                throw new NotImplementedException();
            }
        }

        public override int GetHashCode() {
            return this.SignaturePath.GetHashCode();
        }

        public override string ToString() {
            return this.SignaturePath.Segments.Last();
        }

        public override bool Equals(object other) {
            return this.Equals(other as ITrophyType);
        }

        public bool Equals([AllowNull] ITrophyType other) {
            return other is NamedType type && this.SignaturePath == type.SignaturePath;
        }
    }
}