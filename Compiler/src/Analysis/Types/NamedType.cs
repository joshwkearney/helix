using System;
using System.Linq;

namespace Trophy.Analysis.Types {
    public class NamedType : TrophyType {
        public IdentifierPath SignaturePath { get; }

        public NamedType(IdentifierPath path) {
            this.SignaturePath = path;
        }

        public override IOption<IdentifierPath> AsNamedType() {
            return Option.Some(this.SignaturePath);
        }

        public override bool Equals(object other) {
            return other is NamedType type && this.SignaturePath == type.SignaturePath;
        }

        public override bool HasDefaultValue(ITypeRecorder types) {
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

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
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
            return this.SignaturePath.ToString();
        }
    }
}