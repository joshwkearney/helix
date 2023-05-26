using Helix.Analysis.Flow;
using Helix.Analysis.Predicates;
using Helix.Collections;
using Helix.Parsing;
using Helix.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Analysis.Types {
    public record class PredicateBool : HelixType {
        public ISyntaxPredicate Predicate { get; }

        public PredicateBool(ISyntaxPredicate predicate) {
            this.Predicate = predicate;
        }

        public override PassingSemantics GetSemantics(ITypedFrame types) => PassingSemantics.ValueType;

        public override HelixType GetMutationSupertype(ITypedFrame types) => PrimitiveType.Bool;

        public override HelixType GetSignatureSupertype(ITypedFrame types) => PrimitiveType.Bool;
    }
}
