using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Collections;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree AssignmentStatement() {
            var start = this.TopExpression();

            if (this.TryAdvance(TokenKind.Assignment)) {
                var assign = this.TopExpression();
                var loc = start.Location.Span(assign.Location);
                var result = new AssignmentStatement(loc, start, assign);

                return result;
            }
            else {
                BinaryOperationKind op;

                if (this.TryAdvance(TokenKind.PlusAssignment)) {
                    op = BinaryOperationKind.Add;
                }
                else if (this.TryAdvance(TokenKind.MinusAssignment)) {
                    op = BinaryOperationKind.Subtract;
                }
                else if (this.TryAdvance(TokenKind.StarAssignment)) {
                    op = BinaryOperationKind.Multiply;
                }
                else if (this.TryAdvance(TokenKind.DivideAssignment)) {
                    op = BinaryOperationKind.FloorDivide;
                }
                else if (this.TryAdvance(TokenKind.ModuloAssignment)) {
                    op = BinaryOperationKind.Modulo;
                }
                else {
                    return start;
                }

                var second = this.TopExpression();
                var loc = start.Location.Span(second.Location);
                var assign = new BinarySyntax(loc, start, second, op);
                var stat = new AssignmentStatement(loc, start, assign);

                return stat;
            }
        }
    }
}

namespace Helix.Features.Variables {
    public record AssignmentStatement : ISyntaxTree {
        private readonly ISyntaxTree target, assign;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target, this.assign };

        public bool IsPure => false;

        public AssignmentStatement(
            TokenLocation loc,
            ISyntaxTree target,
            ISyntaxTree assign) {

            this.Location = loc;
            this.target = target;
            this.assign = assign;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (types.ReturnTypes.ContainsKey(this)) {
                return this;
            }

            var target = this.target.CheckTypes(types).ToLValue(types);

            var varSig = target.GetReturnType(types)
                .AsVariable(types)
                .GetValue()
                .InnerType
                .GetMutationSupertype(types);

            var assign = this.assign
                .CheckTypes(types)
                .ToRValue(types);

            var assignType = assign.GetReturnType(types);

            assign = assign.UnifyTo(varSig, types);

            var result = new AssignmentStatement(
                this.Location,
                target,
                assign);

            result.SetReturnType(PrimitiveType.Void, types);
            result.SetCapturedVariables(target, assign, types);
            result.SetPredicate(target, assign, types);
            result.SetLifetimes(AnalyzeFlow(this.Location, target, assign, assignType, types), types);

            return result;
        }

        public static LifetimeBounds AnalyzeFlow(TokenLocation loc, ISyntaxTree target, 
                                                 ISyntaxTree assign, HelixType assignType, TypeFrame flow) {
            var targetBounds = target.GetLifetimes(flow);
            var assignBounds = assign.GetLifetimes(flow);

            // Check to see if the assigned value has the same origins
            // (or more restricted origins) than the target expression.
            // If the origins are compatible, we can assign with no further
            // issue. If they are different, compile error and make the user
            // clarify regions in the signature
            var targetLocation = targetBounds.LocationLifetime;
            var assignLifetime = assignBounds.ValueLifetime;

            // TODO: Redo this
            foreach (var assignRoot in flow.GetMaximumRoots(assignLifetime)) {
                foreach (var targetRoot in flow.GetMaximumRoots(targetLocation)) {
                    if (flow.DataFlowGraph.DoesOutlive(assignRoot, targetRoot)) {
                        continue;
                    }

                    throw new LifetimeException(
                        loc,
                        "Unsafe Memory Store",
                        $"Unable to verify that the assigned value outlives its container. " +
                        $"The region '{assignRoot}' is not known to outlive the region '{targetRoot}', " +
                        $"so this assignment cannot proceed safely. \n\nTo resolve this error, " +
                        $"you can try implementing a '.copy()' method on the type '{assignType}' to allow " +
                        $"its values to be copied between regions, or you can try adding explict " +
                        $"region annotations to your code.");
                }
            }

            var targetType = target.GetReturnType(flow);
            if (targetType is NominalType nom && nom.Kind == NominalTypeKind.Variable) {
                // Here we are storing into a known local variable, so we can replace its
                // current value instead of adding more lifetimes to it. This is the
                // best-case scenario because any lifetime inferences based on the previous
                // value will not depend on future use of this variable
                var newValue = targetBounds.ValueLifetime.IncrementVersion();
                var newTargetBounds = targetBounds.WithValue(newValue);
                var newLocal = new LocalInfo(new PointerType(assignType, true), newTargetBounds);

                // Update this variable's value
                flow.LocalValues = flow.LocalValues.SetItem(newValue.Path, newLocal);

                // Make sure the new value outlives its variable
                flow.DataFlowGraph.AddStored(newValue, newTargetBounds.LocationLifetime);

                targetBounds = newTargetBounds;
            }

            // Add dependencies between our new target and the assignment lifetimes
            flow.DataFlowGraph.AddStored(assignLifetime, targetBounds.LocationLifetime);
            flow.DataFlowGraph.AddAssignment(assignLifetime, targetBounds.ValueLifetime);

            return new LifetimeBounds();
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            // We need to be type checked to be an r-value
            if (!types.ReturnTypes.ContainsKey(this)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var target = new CPointerDereference() {
                Target = this.target.GenerateCode(types, writer)
            };

            var assign = this.assign.GenerateCode(types, writer);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Assignment statement");

            writer.WriteStatement(new CAssignment() {
                Left = target,
                Right = assign
            });

            writer.WriteEmptyLine();

            return new CIntLiteral(0);
        }
    }
}