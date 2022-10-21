using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Generation;
using Trophy.Generation.Syntax;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        public ISyntaxTree BreakStatement(BlockBuilder block) {
            Token start;
            bool isBreak;

            if (this.Peek(TokenKind.BreakKeyword)) {
                start = this.Advance(TokenKind.BreakKeyword);
                isBreak = true;
            }
            else {
                start = this.Advance(TokenKind.ContinueKeyword);
                isBreak = false;
            }

            if (!this.isInLoop.Peek()) {
                throw new ParseException(
                    start.Location, 
                    "Invalid Statement", 
                    "Break and continue statements must only appear inside of loops");
            }

            block.Statements.Add(new BreakContinueSyntax(start.Location, isBreak));
            return new VoidLiteral(start.Location);
        }
    }
}

namespace Trophy.Features.FlowControl {
    public record BreakContinueSyntax : ISyntaxTree {
        private bool isbreak;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => false;

        public BreakContinueSyntax(TokenLocation loc, bool isbreak) {
            this.Location = loc;
            this.isbreak = isbreak;
        }

        public bool RewriteNonlocalFlow(SyntaxFrame types, FlowRewriter flow) {
            var state = flow.NextState++;

            flow.ConstantStates[state] = new ConstantState() {
                Expression = new VoidLiteral(this.Location),
                NextState = this.isbreak ? flow.BreakState : flow.ContinueState
            };

            return true;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
