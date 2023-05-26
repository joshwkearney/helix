using Helix.Analysis.TypeChecking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Analysis.Types {
    public record UnionType(IReadOnlyList<StructMember> Members) : HelixType {
        public override HelixType GetMutationSupertype(ITypeContext types) => this;

        public override HelixType GetSignatureSupertype(ITypeContext types) => this;

        public override PassingSemantics GetSemantics(ITypeContext types) {
            if (this.Members.All(x => x.Type.GetSemantics(types) == PassingSemantics.ValueType)) {
                return PassingSemantics.ValueType;
            }
            else {
                return PassingSemantics.ContainsReferenceType;
            }
        }
    }
}