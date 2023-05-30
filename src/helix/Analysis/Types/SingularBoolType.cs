using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Analysis.Predicates;

namespace Helix.Analysis.Types {
    public record class PredicateBool : HelixType {
        public ISyntaxPredicate Predicate { get; }

        public PredicateBool(ISyntaxPredicate predicate) {
            this.Predicate = predicate;
        }

        public override PassingSemantics GetSemantics(TypeFrame types) => PassingSemantics.ValueType;

        public override HelixType GetMutationSupertype(TypeFrame types) => PrimitiveType.Bool;

        public override HelixType GetSignatureSupertype(TypeFrame types) => PrimitiveType.Bool;
    }

    public record SingularBoolType : PredicateBool {
        public bool Value { get; }

        public SingularBoolType(bool value) : this(value, ISyntaxPredicate.Empty) { }

        public SingularBoolType(bool value, ISyntaxPredicate pred) : base(pred) {
            this.Value = value;
        }

        public override Option<ISyntaxTree> ToSyntax(TokenLocation loc, TypeFrame types) {
            return new BoolLiteral(loc, this.Value);
        }      

        public override string ToString() => this.Value.ToString().ToLower();
    }
}