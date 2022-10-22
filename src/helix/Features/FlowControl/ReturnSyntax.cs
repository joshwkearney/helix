using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Parsing {
    public partial class Parser {
        public ISyntaxTree ReturnStatement(BlockBuilder block) {
            var start = this.Advance(TokenKind.ReturnKeyword);
            var arg = this.TopExpression(block);

            block.Statements.Add(new ReturnSyntax(start.Location, arg));
            return new VoidLiteral(start.Location);
        }
    }
}

namespace Helix.Features.FlowControl {
    public record ReturnSyntax : ISyntaxTree, IStatement {
        private readonly ISyntaxTree payload;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.payload };

        public bool IsPure => false;

        public ReturnSyntax(TokenLocation loc, ISyntaxTree payload) {
            this.Location = loc;
            this.payload = payload;
        }

        public bool RewriteNonlocalFlow(SyntaxFrame types, FlowRewriter flow) {
            var state = flow.NextState++;

            flow.ConstantStates[state] = new ConstantState() {
                Expression = new AssignmentStatement(
                    this.Location,
                    new VariableAccessParseSyntax(this.Location, "$return"),
                    this.payload),
                NextState = flow.ReturnState
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
