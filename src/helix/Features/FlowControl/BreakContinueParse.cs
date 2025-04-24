using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Features.FlowControl;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Parsing {
    public partial class Parser {
        public IParseSyntax BreakStatement() {
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

            return new BreakContinueParse(start.Location, isBreak);
        }
    }
}

namespace Helix.Features.FlowControl {
    public record BreakContinueParse : IParseSyntax {
        private readonly bool isbreak;

        public TokenLocation Location { get; }

        public IEnumerable<IParseSyntax> Children => Enumerable.Empty<IParseSyntax>();

        public bool IsPure => false;

        public BreakContinueParse(TokenLocation loc, bool isbreak) {
            this.Location = loc;
            this.isbreak = isbreak;
        }

        public IParseSyntax ToRValue(TypeFrame types) => this;

        public IParseSyntax CheckTypes(TypeFrame types) {
            types.SyntaxTags[this] = new SyntaxTagBuilder(types).Build();

            return this;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            if (this.isbreak) {
                writer.WriteStatement(new CBreak());
            }
            else {
                writer.WriteStatement(new CContinue());
            }

            return new CIntLiteral(0);
        }
    }
}
