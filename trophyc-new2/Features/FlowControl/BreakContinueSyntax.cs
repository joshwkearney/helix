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
            var start = this.Advance(TokenKind.BreakKeyword);

            block.Statements.Add(new BreakContinueSyntax(start.Location, true));
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
                NextState = flow.BreakState
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
