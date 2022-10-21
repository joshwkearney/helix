using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.Syntax;
using Trophy.Parsing;

namespace Trophy.Features.FlowControl {
    public record BreakContinueSyntax : ISyntaxTree {
        private bool isbreak;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public BreakContinueSyntax(TokenLocation loc, bool isbreak) {
            this.Location = loc;
            this.isbreak = isbreak;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            if (!types.InLoop) {
                var kind = this.isbreak ? "Break" : "Continue";

                throw new TypeCheckingException(
                    this.Location,
                    "Invalid " + kind + " Statement",
                    "Break and continue statements are not allowed outside of loops");
            }

            types.ReturnTypes[this] = PrimitiveType.Void;
            return this;
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
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
