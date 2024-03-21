using Helix.Common;
using Helix.Common.Hmm;
using Helix.Common.Types;
using Helix.MiddleEnd.TypeChecking;
using Helix.MiddleEnd.TypeVisitors;

namespace Helix.MiddleEnd.Interpreting {
    internal class Evaluator {
        public readonly AnalysisContext context;

        public Evaluator(AnalysisContext context) {
            this.context = context;
        }

        public bool TryEvaluateLoop(HmmLoopSyntax syntax, out TypeCheckResult result) {
            var firstIteration = true;

            while (true) {
                this.context.ControlFlowStack.Push(this.context.ControlFlowStack.Peek().CreateLoopFrame());
                this.context.WriterStack.Push(this.context.Writer.CreateScope());
                this.context.ScopeStack.Push(this.context.ScopeStack.Peek().CreateScope());

                // Rewrite the loop body with different variables so different iterations don't
                // conflict with each other when unrolled
                var rewriter = new HmmNameRewriter(this.context.Names.GetLoopUnrollPrefix());

                var rewrittenBody = syntax.Body
                    .Select(x => x.Accept(rewriter))
                    .ToArray();

                // Type check the body
                var bodyResult = this.context.TypeChecker.CheckBody(rewrittenBody);

                var loopScope = this.context.ScopeStack.Pop();
                var bodyWriter = this.context.WriterStack.Pop();
                var loopControlFlow = this.context.ControlFlowStack.Peek();

                if (!bodyResult.ControlFlow.DoesJump && bodyResult.ControlFlow.CouldJump) {
                    // Here we have normal control flow until the end of the loop but don't know
                    // if the body contains a jump, which means we need to stop evaluating
                    // because the loop break can no longer be determined at compile time

                    if (firstIteration) {
                        // No iterations were able to be unrolled, so fail. Luckily any type
                        // or alias modifications went into the new scopes so we are already
                        // rolled back

                        result = default;
                        return false;
                    }
                    else {
                        result = syntax.Accept(this.context.TypeChecker);
                        return true;
                    }
                }

                // We either definitely return or fall through the loop to iterate again,
                // so we can unroll this iteration

                // Replace the current aliases with our aliases
                this.context.ScopeStack.Pop();
                this.context.ScopeStack.Push(loopScope);

                // Remove any residual breaks and continues
                var replacedBody = bodyWriter.ScopedLines.SelectMany(x => x.Accept(LoopEvalJumpRemover.Instance)).ToArray();

                // Write the body
                foreach (var line in replacedBody) {
                    this.context.Writer.AddLine(line);
                }

                // If this iteration broke the loop, stop eveluating
                if (bodyResult.ControlFlow.DoesFunctionReturn) {
                    result = new TypeCheckResult("void", ControlFlow.FunctionReturnFlow);
                    return true;
                }
                else if (bodyResult.ControlFlow.DoesBreak) {
                    result = new TypeCheckResult("void", ControlFlow.NormalFlow);
                    return true;
                }

                firstIteration = false;
            }            
        }

        public bool TryEvaluateIfExpression(HmmIfExpression syntax, string cond, out TypeCheckResult result) {
            var condType = this.context.Types[cond];
            
            if (condType is not SingularBoolType boolType) {
                if (this.context.Predicates[cond].IsTrue) {
                    boolType = new SingularBoolType(true);
                }
                else if (this.context.Predicates[cond].IsFalse) {
                    boolType = new SingularBoolType(false);
                }
                else {
                    result = default;
                    return false;
                }
            }

            if (boolType.Value) {
                var affirmFlow = this.context.TypeChecker.CheckBody(syntax.AffirmativeBody);

                if (affirmFlow.ControlFlow.DoesJump) {
                    result = affirmFlow;
                    return true;
                }

                var assign = new HmmVariableStatement() {
                    Location = syntax.Location,
                    IsMutable = false,
                    Variable = syntax.Result,
                    Value = syntax.Affirmative
                };

                result = assign.Accept(this.context.TypeChecker);
                return true;
            }
            else {
                var affirmFlow = this.context.TypeChecker.CheckBody(syntax.NegativeBody);

                if (affirmFlow.ControlFlow.DoesJump) {
                    result = affirmFlow;
                    return true;
                }

                var assign = new HmmVariableStatement() {
                    Location = syntax.Location,
                    IsMutable = false,
                    Variable = syntax.Result,
                    Value = syntax.Negative
                };

                result = assign.Accept(this.context.TypeChecker);
                return true;
            }
        }

        public bool TryEvaluateUnarySyntax(HmmUnaryOperator syntax, out TypeCheckResult result) {
            var type = this.context.Types[syntax.Operand];

            if (type is SingularBoolType boolType && syntax.Operator == UnaryOperatorKind.Not) {
                var stat = new HmmVariableStatement() {
                    Location = syntax.Location,
                    IsMutable = false,
                    Variable = syntax.Result,
                    Value = (!boolType.Value).ToString().ToLower()
                };

                result = stat.Accept(this.context.TypeChecker);
                return true;
            }

            result = default;
            return false;
        }

        public bool TryEvaluateVisitBinarySyntax(HmmBinarySyntax syntax, out TypeCheckResult resultName) {
            var leftType = this.context.Types[syntax.Left];
            var rightType = this.context.Types[syntax.Right];

            // Evaluate word operations when we know both sides
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

            // Evaluate book operations when we know both sides
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
            

            // Evaluate bool operations when we know one side
            if (leftType is SingularBoolType boolLeft2) {
                return TryCheckSingleBinaryExpression(syntax, boolLeft2.Value, syntax.Right, out resultName);
            }
            else if (rightType is SingularBoolType boolRight2) {
                return TryCheckSingleBinaryExpression(syntax, boolRight2.Value, syntax.Left, out resultName);
            }

            resultName = default;
            return false;
        }

        bool TryCheckSingleBinaryExpression(HmmBinarySyntax syntax, bool value, string other, out TypeCheckResult result) {
            if (syntax.Operator == BinaryOperationKind.Or) {
                if (value) {
                    var stat = new HmmVariableStatement() {
                        IsMutable = false,
                        Location = syntax.Location,
                        Value = "true",
                        Variable = syntax.Result
                    };

                    result = stat.Accept(this.context.TypeChecker);
                    return true;
                }
                else {
                    var stat = new HmmVariableStatement() {
                        IsMutable = false,
                        Location = syntax.Location,
                        Value = other,
                        Variable = syntax.Result
                    };

                    result = stat.Accept(this.context.TypeChecker);
                    return true;
                }
            }
            else if (syntax.Operator == BinaryOperationKind.And) {
                if (value) {
                    var stat = new HmmVariableStatement() {
                        IsMutable = false,
                        Location = syntax.Location,
                        Value = other,
                        Variable = syntax.Result
                    };

                    result = stat.Accept(this.context.TypeChecker);
                    return true;
                }
                else {
                    var stat = new HmmVariableStatement() {
                        IsMutable = false,
                        Location = syntax.Location,
                        Value = "false",
                        Variable = syntax.Result
                    };

                    result = stat.Accept(this.context.TypeChecker);
                    return true;
                }
            }

            result = default;
            return false;
        }

        public bool TryEvaluateRValueStructMemberAccess(HmmMemberAccess syntax, StructType structType, out TypeCheckResult result) {
            Assert.IsTrue(structType.Members.Any(x => x.Name == syntax.Member));

            var loc = new MemberAccessLocation() {
                Parent = new NamedLocation(syntax.Operand),
                Member = syntax.Member
            };

            var type = this.context.Types[loc];

            if (type.Accept(TypeToExpressionVisitor.Instance).TryGetValue(out var expr)) {
                var stat = new HmmVariableStatement() {
                    IsMutable = false,
                    Location = syntax.Location,
                    Value = expr,
                    Variable = syntax.Result
                };

                result = stat.Accept(this.context.TypeChecker);
                return true;
            }

            result = default;
            return false;
        }

        public bool TryEvaluateRValueDereference(HmmDereference syntax, out TypeCheckResult result) {
            var pointerType = this.context.Types[syntax.Operand];
            var lvalues = this.context.Aliases.GetBoxedRoots(new NamedLocation(syntax.Operand), pointerType);

            if (lvalues.Count != 1 || lvalues.First().IsUnknown) {
                result = default;
                return false;
            }

            var type = this.context.Types[lvalues.First()];

            if (!type.Accept(TypeToExpressionVisitor.Instance).TryGetValue(out var expr)) {
                result = default;
                return false;
            }

            var stat = new HmmVariableStatement() {
                IsMutable = false,
                Location = syntax.Location,
                Value = expr,
                Variable = syntax.Result
            };

            result = stat.Accept(this.context.TypeChecker);
            return true;
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
