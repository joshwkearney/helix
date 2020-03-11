using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt17.Features.FlowControl {
    public class FlowControlTypeChecker {
        private readonly Stack<int> blockId = new Stack<int>();

        public FlowControlTypeChecker() {
            this.blockId.Push(0);
        }

        public ISyntax<TypeCheckTag> CheckWhileSyntax(WhileSyntax<ParseTag> syntax, ITypeCheckScope scope, ITypeChecker checker) {
            var cond = checker.Check(syntax.Condition, scope);
            var body = checker.Check(syntax.Body, scope);
            var tag = new TypeCheckTag(VoidType.Instance);

            if (cond.Tag.ReturnType != BoolType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(
                    syntax.Condition.Tag.Location,
                    IntType.Instance, cond.Tag.ReturnType);
            }

            return new WhileSyntax<TypeCheckTag>(tag, cond, body);
        }

        public ISyntax<TypeCheckTag> CheckIfSyntax(IfSyntax<ParseTag> syntax, ITypeCheckScope scope, ITypeChecker checker) {
            var cond = checker.Check(syntax.Condition, scope);
            var affirm = checker.Check(syntax.Affirmative, scope);
            var neg = syntax.Negative.Select(x => checker.Check(x, scope));

            // Make sure that the condition is an integet (later a boolean)
            if (cond.Tag.ReturnType != BoolType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(syntax.Condition.Tag.Location, IntType.Instance, cond.Tag.ReturnType);
            }

            // Make sure the branches match types
            if (syntax.Kind == IfKind.Expression) {
                if (affirm.Tag.ReturnType != neg.GetValue().Tag.ReturnType) {
                    throw TypeCheckingErrors.UnexpectedType(
                        syntax.Negative.GetValue().Tag.Location,
                        affirm.Tag.ReturnType,
                        neg.GetValue().Tag.ReturnType);
                }
            }

            // Get the type checking tag
            TypeCheckTag tag;
            if (syntax.Kind == IfKind.Expression) {
                tag = new TypeCheckTag(
                    affirm.Tag.ReturnType,
                    affirm.Tag.CapturedVariables.Union(neg.GetValue().Tag.CapturedVariables));
            }
            else {
                tag = new TypeCheckTag(VoidType.Instance);
            }

            return new IfSyntax<TypeCheckTag>(tag, syntax.Kind, cond, affirm, neg);
        }

        public ISyntax<TypeCheckTag> CheckBlockSyntax(BlockSyntax<ParseTag> syntax, ITypeCheckScope scope, ITypeChecker checker) {
            // Get the id for this scope
            var id = this.blockId.Pop();
            var blockPath = scope.Path.Append("block" + id);

            // Increment the blockId for the next scope
            this.blockId.Push(id + 1);

            // Reset the blockId for scopes within this scope
            this.blockId.Push(0);

            // Get a new scope for analyzing the statements
            var blockScope = new BlockScope(scope.Path.Append("block" + id), scope);

            // Analyze the statements
            var stats = ImmutableList<ISyntax<TypeCheckTag>>.Empty;

            foreach (var stat in syntax.Statements) {
                var checkedStat = checker.Check(stat, blockScope);

                stats = stats.Add(checkedStat);
            }

            // Make sure we're not about to return a value that's dependent on variables
            // within this scope
            if (stats.Any()) {
                var last = stats.Last();

                foreach (var path in last.Tag.CapturedVariables.Select(x => x.Path)) {
                    if (path.StartsWith(blockPath)) {
                        throw TypeCheckingErrors.VariableScopeExceeded(syntax.Statements.Last().Tag.Location, path);
                    }
                }

                return new BlockSyntax<TypeCheckTag>(last.Tag, stats);
            }
            else {
                var tag = new TypeCheckTag(VoidType.Instance);

                return new BlockSyntax<TypeCheckTag>(tag, stats);
            }
        }
    }
}