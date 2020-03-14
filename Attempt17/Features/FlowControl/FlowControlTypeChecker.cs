using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.FlowControl {
    public class FlowControlTypeChecker
        : IFlowControlVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> {

        private readonly Stack<int> blockId = new Stack<int>();

        public FlowControlTypeChecker() {
            this.blockId.Push(0);
        }

        public ISyntax<TypeCheckTag> VisitBlock(BlockSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            // Get the id for this scope
            var id = this.blockId.Pop();
            var blockPath = context.Scope.Path.Append("block" + id);

            // Increment the blockId for the next scope
            this.blockId.Push(id + 1);

            // Reset the blockId for scopes within this scope
            this.blockId.Push(0);

            // Get a new scope for analyzing the statements
            var blockScope = new BlockScope(blockPath, context.Scope);
            context = context.WithScope(blockScope);

            // Analyze the statements
            var stats = ImmutableList<ISyntax<TypeCheckTag>>.Empty;

            foreach (var stat in syntax.Statements) {
                var checkedStat = stat.Accept(visitor, context);

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

        public ISyntax<TypeCheckTag> VisitIf(IfSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var cond = syntax.Condition.Accept(visitor, context);
            var affirm = syntax.Affirmative.Accept(visitor, context);
            var neg = syntax.Negative.Select(x => x.Accept(visitor, context));

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

        public ISyntax<TypeCheckTag> VisitWhile(WhileSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var cond = syntax.Condition.Accept(visitor, context);
            var body = syntax.Body.Accept(visitor, context);
            var tag = new TypeCheckTag(VoidType.Instance);

            // Make sure the condition is a boolean
            if (cond.Tag.ReturnType != BoolType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(
                    syntax.Condition.Tag.Location,
                    IntType.Instance, cond.Tag.ReturnType);
            }

            return new WhileSyntax<TypeCheckTag>(tag, cond, body);
        }
    }
}
