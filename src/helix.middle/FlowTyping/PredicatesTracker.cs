using Helix.Common;
using Helix.Common.Hmm;
using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.FlowTyping {
    internal class PredicatesTracker {
        private readonly AnalysisContext context;

        public PredicatesTracker(AnalysisContext context) {
            this.context = context;
        }

        public void TrackBinaryExpression(HmmBinarySyntax syntax) {
            if (syntax.Operator == BinaryOperationKind.EqualTo || syntax.Operator == BinaryOperationKind.NotEqualTo) {
                this.TrackEquals(syntax);
            }
            else if (syntax.Operator == BinaryOperationKind.And) {
                this.TrackAnd(syntax);
            }
            else if (syntax.Operator == BinaryOperationKind.Or) {
                this.TrackOr(syntax);
            }
            else if (syntax.Operator == BinaryOperationKind.Xor) {
                this.TrackXor(syntax);
            }
        }

        private void TrackXor(HmmBinarySyntax syntax) {
            var finalLocation = new NamedLocation(syntax.Result);
            var left = this.context.Predicates[syntax.Left];
            var right = this.context.Predicates[syntax.Right];

            this.context.Predicates[finalLocation] = (left | right) & (!left | !right);
        }

        private void TrackXnor(HmmBinarySyntax syntax) {
            var finalLocation = new NamedLocation(syntax.Result);
            var left = this.context.Predicates[syntax.Left];
            var right = this.context.Predicates[syntax.Right];

            // TODO: If one of these is empty this should be false
            this.context.Predicates[finalLocation] = (left | !right) & (!left | right);
        }

        private void TrackOr(HmmBinarySyntax syntax) {
            var finalLocation = new NamedLocation(syntax.Result);

            this.context.Predicates[finalLocation] = this.context.Predicates[syntax.Left].Or(this.context.Predicates[syntax.Right]);
        }

        private void TrackAnd(HmmBinarySyntax syntax) {
            var finalLocation = new NamedLocation(syntax.Result);

            this.context.Predicates[finalLocation] = this.context.Predicates[syntax.Left].And(this.context.Predicates[syntax.Right]);
        }

        private void TrackEquals(HmmBinarySyntax syntax) {
            var isNegated = syntax.Operator == BinaryOperationKind.NotEqualTo;
            var leftType = this.context.Types[syntax.Left];
            var rightType = this.context.Types[syntax.Right];

            if (leftType is SingularWordType wordType && rightType.GetSupertype() == WordType.Instance) {
                this.TrackWordEqualsHelper(syntax.Result, wordType, syntax.Right, isNegated);
            }
            else if (rightType is SingularWordType wordType1 && leftType.GetSupertype() == WordType.Instance) {
                this.TrackWordEqualsHelper(syntax.Result, wordType1, syntax.Left, isNegated);
            }
            else if (leftType.GetSupertype() is BoolType && rightType.GetSupertype() is BoolType) {
                if (isNegated) {
                    this.TrackXor(syntax);
                }
                else {
                    this.TrackXnor(syntax);
                }
            }
        }

        private void TrackWordEqualsHelper(string resultName, SingularWordType word, string otherName, bool isNegated) {
            var roots = this.context.Aliases.GetReferencedRoots(new NamedLocation(otherName));
            
            if (roots.Count == 1) {
                var root = roots.First();
                var finalLocation = new NamedLocation(resultName);

                this.context.Predicates[finalLocation] = (CnfTerm)new WordVariablePredicate(root, word.Value, isNegated);
            }
        }
    }
}
