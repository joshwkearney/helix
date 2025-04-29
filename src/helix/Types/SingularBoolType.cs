using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Primitives;
using Helix.TypeChecking;
using Helix.TypeChecking.Predicates;

namespace Helix.Types {
    public record PredicateBool : HelixType {
        public ISyntaxPredicate Predicate { get; }

        public PredicateBool(ISyntaxPredicate predicate) {
            this.Predicate = predicate;
        }

        public override PassingSemantics GetSemantics(TypeFrame types) => PassingSemantics.ValueType;

        public override HelixType GetSignature(TypeFrame types) => PrimitiveType.Bool;

        public override bool IsBool(TypeFrame types) => true;
    }

    public record SingularBoolType : PredicateBool {
        public bool Value { get; }

        public SingularBoolType(bool value) : this(value, ISyntaxPredicate.Empty) { }

        public SingularBoolType(bool value, ISyntaxPredicate pred) : base(pred) {
            this.Value = value;
        }

        public override Option<ITypedExpression> ToSyntax(TokenLocation loc, TypeFrame types) {
            return new BoolLiteral {
                Location = loc, 
                Value = this.Value
            };
        }      

        public override string ToString() => this.Value.ToString().ToLower();
    }
}