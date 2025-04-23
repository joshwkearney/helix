﻿using Helix.Analysis.TypeChecking;

namespace Helix.Analysis.Types {
    public record StructType(IReadOnlyList<StructMember> Members) : HelixType {
        public override HelixType GetMutationSupertype(TypeFrame types) => this;

        public override HelixType GetSignatureSupertype(TypeFrame types) => this;

        public override PassingSemantics GetSemantics(TypeFrame types) {
            if (this.Members.All(x => x.Type.GetSemantics(types) == PassingSemantics.ValueType)) {
                return PassingSemantics.ValueType;
            }
            else {
                return PassingSemantics.ContainsReferenceType;
            }
        }

        public override IEnumerable<HelixType> GetAccessibleTypes(TypeFrame frame) {
            yield return this;

            foreach (var mem in this.Members) {
                foreach (var type in mem.Type.GetAccessibleTypes(frame)) {
                    yield return type;
                }
            }
        }
    }

    public record StructMember(string Name, HelixType Type) { }
}