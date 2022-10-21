﻿using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.FlowControl;
using Trophy.Parsing;
using Trophy.Generation.Syntax;
using Trophy.Features.Primitives;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree WhileStatement(BlockBuilder block) {
            var newBlock = new BlockBuilder();
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression(newBlock);

            var test = new IfParseSyntax(
                cond.Location,
                this.scope.Append(block.GetTempName()),
                new UnaryParseSyntax(cond.Location, UnaryOperatorKind.Not, cond),
                new BreakContinueSyntax(cond.Location, true));

            // False loops will never run and true loops don't need a break test
            if (cond is not Features.Primitives.BoolLiteral) {
                newBlock.Statements.Add(test);
            }

            this.Advance(TokenKind.DoKeyword);

            this.isInLoop.Push(true);
            var body = this.TopExpression(newBlock);
            this.isInLoop.Pop();

            var loc = start.Location.Span(body.Location);
            var loop = new LoopStatement(loc, new BlockSyntax(loc, newBlock.Statements));

            block.Statements.Add(loop);

            return new VoidLiteral(loc);
        }
    }
}

namespace Trophy.Features.FlowControl {
    public record LoopStatement : ISyntaxTree, IStatement {
        private static int counter = 0;

        private readonly BlockSyntax body;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.body };

        public bool IsPure => false;

        public LoopStatement(TokenLocation location,
                             BlockSyntax body, bool isTypeChecked = false) {

            this.Location = location;
            this.body = body;
            this.isTypeChecked = isTypeChecked;
        }

        public bool RewriteNonlocalFlow(SyntaxFrame types, FlowRewriter flow) {
            int oldBreakState = flow.BreakState;
            int oldContinueState = flow.ContinueState;

            int loopState = flow.NextState++;
            int breakState = flow.NextState++;

            flow.ContinueState = loopState;
            flow.BreakState = breakState;

            // The main loop state should just jump to the next state
            flow.ConstantStates[loopState] = new ConstantState() {
                Expression = new VoidLiteral(this.Location),
                NextState = flow.NextState
            };

            // Generate states for the loop body
            this.body.RewriteNonlocalFlow(types, flow);

            // The last state after rewriting the body should go back to the 
            // beginning of the loop
            int end = flow.NextState++;
            flow.ConstantStates[end] = new ConstantState() {
                Expression = new VoidLiteral(this.Location),
                NextState = loopState
            };

            // Wire up the break state to jump out of the loop
            flow.ConstantStates[breakState] = new ConstantState() {
                Expression = new VoidLiteral(this.Location),
                NextState = end + 1
            };

            flow.BreakState = oldBreakState;
            flow.ContinueState = oldContinueState;

            return true;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public Option<ISyntaxTree> ToRValue(SyntaxFrame types) {
            throw new InvalidOperationException();

        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
