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
            var targetType = ((PointerType)target.GetReturnType(types)).InnerType;

            var assign = this.assign
                .CheckTypes(types)
                .ToRValue(types)
                .ConvertTypeTo(targetType, types);            

            var result = new AssignmentStatement(
                this.Location,
                target,
                assign);

            result.SetReturnType(PrimitiveType.Void, types);
            result.SetCapturedVariables(target, assign, types);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            // We need to be type checked to be an r-value
            if (!types.ReturnTypes.ContainsKey(this)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (flow.SyntaxLifetimes.ContainsKey(this)) {
                return;
            }

            this.target.AnalyzeFlow(flow);
            this.assign.AnalyzeFlow(flow);

            var targetBundle = this.target.GetLifetimes(flow);
            var assignBundle = this.assign.GetLifetimes(flow);

            // Check to see if the assigned value has the same origins
            // (or more restricted origins) than the target expression.
            // If the origins are compatible, we can assign with no further
            // issue. If they are different, compile error and make the user
            // clarify regions in the signature
            foreach (var (path, type) in this.assign.GetReturnType(flow).GetMembers(flow)) {
                var targetLocation = targetBundle[path].LocationLifetime;
                var assignLifetime = assignBundle[path].ValueLifetime;

                // TODO: Redo this
                foreach (var assignRoot in flow.GetMaximumRoots(assignLifetime)) {
                    foreach (var targetRoot in flow.GetMaximumRoots(targetLocation)) {
                        if (flow.LifetimeGraph.DoesOutlive(assignRoot, targetRoot)) {
                            continue;
                        }

                        throw new LifetimeException(
                            this.Location,
                            "Unsafe Memory Store",
                            $"Unable to verify that the assigned value outlives its container. " +
                            $"The region '{assignRoot}' is not known to outlive the region '{targetRoot}', " +
                            $"so this assignment cannot proceed safely. \n\nTo resolve this error, " +
                            $"you can try implementing a '.copy()' method on the type '{type}' to allow " +
                            $"its values to be copied between regions, or you can try adding explict " +
                            $"region annotations to your code.");
                    }
                }
            }

            foreach (var (relPath, memType) in this.assign.GetReturnType(flow).GetMembers(flow)) {
                var targetBounds = targetBundle[relPath];
                var assignLifetime = assignBundle[relPath].ValueLifetime;

                // Skip value types
                if (memType.IsValueType(flow)) {
                    continue;
                }

                if (targetBounds.ValueLifetime.Origin == LifetimeOrigin.LocalValue) {
                    // Here we are storing into a known local variable, so we can replace its
                    // current value instead of adding more lifetimes to it. This is the
                    // best-case scenario because any lifetime inferences based on the previous
                    // value will not depend on future use of this variable
                    var newValue = targetBounds.ValueLifetime.IncrementVersion();
                    var newTargetBounds = targetBounds.WithValue(newValue);

                    // Update this variable's value
                    flow.LocalLifetimes = flow.LocalLifetimes.SetItem(newValue.Path, newTargetBounds);

                    // Make sure the new value outlives its variable
                    flow.LifetimeGraph.AddStored(newValue, newTargetBounds.LocationLifetime, memType);
                }

                // Add dependencies between our new target and the assignment lifetimes
                flow.LifetimeGraph.AddStored(assignLifetime, targetBounds.LocationLifetime, memType);
                flow.LifetimeGraph.AddAssignment(assignLifetime, targetBounds.ValueLifetime, memType);
            }

            this.SetLifetimes(new LifetimeBundle(), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
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