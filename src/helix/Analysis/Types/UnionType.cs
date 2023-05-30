using Helix.Analysis.TypeChecking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Analysis.Types {
    public record UnionType(IReadOnlyList<StructMember> Members) : HelixType {
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
}