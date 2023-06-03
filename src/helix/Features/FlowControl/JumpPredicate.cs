//using Helix.Analysis.Predicates;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Helix.Features.FlowControl {
//    public enum JumpKind { Return, BreakContinue }

//    public class JumpPredicate : SyntaxPredicateLeaf {
//        public JumpKind Kind { get; }

//        public bool IsNegated { get; }

//        public JumpPredicate(JumpKind kind, bool isNegated = false) {
//            this.Kind = kind;
//            this.IsNegated = isNegated;
//        }

//        public override bool Equals(object other) {
//            if (other is JumpPredicate jump) {
//                return this.Kind == jump.Kind && this.IsNegated == jump.IsNegated;
//            }

//            return false;
//        }

//        public override bool Equals(ISyntaxPredicate other) {
//            if (other is JumpPredicate jump) {
//                return this.Kind == jump.Kind && this.IsNegated == jump.IsNegated;
//            }

//            return false;
//        }

//        public override int GetHashCode() => this.Kind.GetHashCode() + 13 * this.IsNegated.GetHashCode();

//        public override ISyntaxPredicate Negate() => new JumpPredicate(this.Kind, !this.IsNegated);

//        public override bool TryAndWith(ISyntaxPredicate other, out ISyntaxPredicate result) {
//            if (other is not JumpPredicate jump || jump.Kind != this.Kind) {
//                result = null;
//                return false;
//            }

//            result = new JumpPredicate(this.Kind, this.IsNegated || jump.IsNegated);
//            return true;
//        }

//        public override bool TryOrWith(ISyntaxPredicate other, out ISyntaxPredicate result) {
//            if (other is not JumpPredicate jump || jump.Kind != this.Kind) {
//                result = null;
//                return false;
//            }

//            result = new JumpPredicate(this.Kind, this.IsNegated && jump.IsNegated);
//            return true;
//        }

//        public override string ToString() {
//            var result = "";
            
//            if (this.IsNegated) {
//                result += "!";
//            }

//            switch (this.Kind) {
//                case JumpKind.Return:
//                    result += "return";
//                    break;
//                case JumpKind.BreakContinue:
//                    result += "breakcontinue";
//                    break;
//            }

//            return result;
//        }
//    }
//}
