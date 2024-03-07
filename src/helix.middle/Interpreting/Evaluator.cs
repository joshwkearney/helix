using Helix.Common;
using Helix.Common.Collections;
using Helix.Common.Hmm;
using Helix.Common.Tokens;
using Helix.Common.Types;
using Helix.MiddleEnd.TypeChecking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.Interpreting {
    internal class Evaluator {
        public readonly TypeCheckingContext context;

        public Evaluator(TypeCheckingContext context) {
            this.context = context;
        }

        public bool TryEvaluateIfExpression(HmmIfExpression syntax, string cond, out TypeCheckResult result) {
            var condType = this.context.Types.GetType(cond);

            if (condType is not SingularBoolType boolType) {
                result = default;
                return false;
            }

            if (boolType.Value) {
                var affirmFlow = this.context.TypeChecker.CheckBody(syntax.AffirmativeBody);

                if (affirmFlow.ControlFlow != StatementControlFlow.Normal) {
                    result = affirmFlow;
                }
                else {
                    result = TypeCheckResult.NormalFlow(syntax.Affirmative);
                }
            }
            else {
                var affirmFlow = this.context.TypeChecker.CheckBody(syntax.NegativeBody);

                if (affirmFlow.ControlFlow != StatementControlFlow.Normal) {
                    result = affirmFlow;
                }
                else {
                    result = TypeCheckResult.NormalFlow(syntax.Negative);
                }
            }

            return true;
        }

        public bool TryEvaluateVisitBinarySyntax(HmmBinarySyntax syntax, out TypeCheckResult resultName) {
            var leftType = this.context.Types.GetType(syntax.Left);
            var rightType = this.context.Types.GetType(syntax.Right);

            if (leftType is SingularWordType wordLeft && rightType is SingularWordType wordRight) {
                IHelixType? result = syntax.Operator switch {
                    BinaryOperationKind.Add => new SingularWordType(wordLeft.Value + wordRight.Value),
                    BinaryOperationKind.Subtract => new SingularWordType(wordLeft.Value - wordRight.Value),
                    BinaryOperationKind.Multiply => new SingularWordType(wordLeft.Value * wordRight.Value),
                    BinaryOperationKind.Modulo => new SingularWordType(wordLeft.Value % wordRight.Value),
                    BinaryOperationKind.FloorDivide => new SingularWordType(wordLeft.Value / wordRight.Value),
                    BinaryOperationKind.And => new SingularWordType(wordLeft.Value & wordRight.Value),
                    BinaryOperationKind.Or => new SingularWordType(wordLeft.Value | wordRight.Value),
                    BinaryOperationKind.Xor => new SingularWordType(wordLeft.Value ^ wordRight.Value),
                    BinaryOperationKind.EqualTo => new SingularBoolType(wordLeft.Value == wordRight.Value),
                    BinaryOperationKind.NotEqualTo => new SingularBoolType(wordLeft.Value != wordRight.Value),
                    BinaryOperationKind.GreaterThan => new SingularBoolType(wordLeft.Value > wordRight.Value),
                    BinaryOperationKind.LessThan => new SingularBoolType(wordLeft.Value < wordRight.Value),
                    BinaryOperationKind.GreaterThanOrEqualTo => new SingularBoolType(wordLeft.Value >= wordRight.Value),
                    BinaryOperationKind.LessThanOrEqualTo => new SingularBoolType(wordLeft.Value <= wordRight.Value),
                    _ => null,
                };

                if (result == null) {
                    resultName = default;
                    return false;
                }

                var stat = new HmmVariableStatement() {
                    IsMutable = false,
                    Location = syntax.Location,
                    Value = result.ToString(),
                    Variable = syntax.Result
                };

                resultName = stat.Accept(this.context.TypeChecker);
                return true;
            }

            if (leftType is SingularBoolType boolLeft && rightType is SingularBoolType boolRight) {
                IHelixType? result = syntax.Operator switch {
                    BinaryOperationKind.And => new SingularBoolType(boolLeft.Value & boolRight.Value),
                    BinaryOperationKind.Or => new SingularBoolType(boolLeft.Value | boolRight.Value),
                    BinaryOperationKind.Xor => new SingularBoolType(boolLeft.Value ^ boolRight.Value),
                    BinaryOperationKind.EqualTo => new SingularBoolType(boolLeft.Value == boolRight.Value),
                    BinaryOperationKind.NotEqualTo => new SingularBoolType(boolLeft.Value != boolRight.Value),
                    _ => null,
                };

                if (result == null) {
                    resultName = default;
                    return false;
                }

                var stat = new HmmVariableStatement() {
                    IsMutable = false,
                    Location = syntax.Location,
                    Value = result.ToString(),
                    Variable = syntax.Result
                };

                resultName = stat.Accept(this.context.TypeChecker);
                return true;
            }

            resultName = default;
            return false;
        }

        public bool TryEvaluateRValueStructMemberAccess(HmmMemberAccess access, StructType structType, out TypeCheckResult result) {
            Assert.IsTrue(structType.Members.Any(x => x.Name == access.Member));

            var loc = new MemberAccessLocation() {
                Parent = new NamedLocation(access.Operand),
                Member = access.Member
            };

            var type = this.context.Types.GetType(loc);

            if (type.Accept(TypeExpressionVisitor.Instance).TryGetValue(out var expr)) {
                var stat = new HmmVariableStatement() {
                    IsMutable = false,
                    Location = access.Location,
                    Value = expr,
                    Variable = access.Result
                };

                result = stat.Accept(this.context.TypeChecker);
                return true;
            }

            result = default;
            return false;
        }

        //public void EvaluateAssignment(HmmAssignment syntax, IReadOnlyDictionary<IValueLocation, ValueSet<IValueLocation>> allTargets) {
        //    foreach (var (loc, targets) in allTargets) {
        //        if (targets.Count == 1 && !targets.First().IsUnknown) {
        //            this.context.Types.SetLocal(loc, )
        //        }
        //    }
        //}
    }
}
