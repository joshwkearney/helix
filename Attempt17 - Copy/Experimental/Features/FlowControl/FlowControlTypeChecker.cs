using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt17.Experimental.Features.FlowControl {
    public class FlowControlTypeChecker {
        private readonly Stack<int> blockId = new Stack<int>();

        public FlowControlTypeChecker() {
            this.blockId.Push(0);
        }

        public ISyntax<TypeCheckInfo> CheckWhileSyntax(WhileSyntax<ParseInfo> syntax, Scope scope, ITypeChecker checker) {
            var cond = checker.Check(syntax.Condition, scope);
            var body = checker.Check(syntax.Body, scope);
            var tag = new TypeCheckInfo(VoidType.Instance);

            if (cond.Tag.ReturnType != IntType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(
                    syntax.Condition.Tag.Location, 
                    IntType.Instance, cond.Tag.ReturnType);
            }

            return new WhileSyntax<TypeCheckInfo>(tag, cond, body);
        }

        public ISyntax<TypeCheckInfo> CheckIfSyntax(IfSyntax<ParseInfo> syntax, Scope scope, ITypeChecker checker) {
            var cond = checker.Check(syntax.Condition, scope);
            var affirm = checker.Check(syntax.Affirmative, scope);
            var neg = syntax.Negative.Select(x => checker.Check(x, scope));

            // Make sure that the condition is an integet (later a boolean)
            if (cond.Tag.ReturnType != IntType.Instance) {
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
            TypeCheckInfo tag;
            if (syntax.Kind == IfKind.Expression) {
                tag = new TypeCheckInfo(
                    affirm.Tag.ReturnType,
                    affirm.Tag.CapturedVariables.Union(neg.GetValue().Tag.CapturedVariables));
            }
            else {
                tag = new TypeCheckInfo(VoidType.Instance);
            }

            return new IfSyntax<TypeCheckInfo>(tag, syntax.Kind, cond, affirm, neg);
        }

        public ISyntax<TypeCheckInfo> CheckBlockSyntax(BlockSyntax<ParseInfo> syntax, Scope scope, ITypeChecker checker) {
            // Get the id for this scope
            var id = this.blockId.Pop();
            var blockPath = scope.Path.Append("block" + id);

            // Increment the blockId for the next scope
            this.blockId.Push(id + 1);

            // Reset the blockId for scopes within this scope
            this.blockId.Push(0);

            // Get a new scope for analyzing the statements
            var blockScope = scope.GetFrame(x => x.Append("block" + id));

            // Analyze the statements
            var stats = ImmutableList<ISyntax<TypeCheckInfo>>.Empty;

            foreach (var stat in syntax.Statements) {
                var checkedStat = checker.Check(stat, blockScope);
                
                stats = stats.Add(checkedStat);
            }

            // Make sure we're not about to return a value that's dependent on variables
            // within this scope
            if (stats.Any()) {
                var last = stats.Last();

                foreach (var var in last.Tag.CapturedVariables) {
                    if (var.StartsWith(blockPath)) {
                        throw TypeCheckingErrors.VariableScopeExceeded(syntax.Statements.Last().Tag.Location, var);
                    }
                }

                return new BlockSyntax<TypeCheckInfo>(last.Tag, stats);
            }
            else {
                var tag = new TypeCheckInfo(VoidType.Instance);

                return new BlockSyntax<TypeCheckInfo>(tag, stats);
            }
        }
    }
}