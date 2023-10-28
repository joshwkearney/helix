using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.HelixMinusMinus;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree VoidLiteral() {
            var tok = this.Advance(TokenKind.VoidKeyword);

            return new VoidLiteral(tok.Location);
        }
    }
}

namespace Helix.Features.Primitives {
    public record VoidLiteral : ISyntaxTree {
        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public VoidLiteral(TokenLocation loc) {
            this.Location = loc;
        }

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public Option<HelixType> AsType(TypeFrame types) => PrimitiveType.Void;

        public ISyntaxTree CheckTypes(TypeFrame types) {
            SyntaxTagBuilder.AtFrame(types).BuildFor(this);

            return this;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CIntLiteral(0);
        }

        public HmmValue GenerateHelixMinusMinus(TypeFrame types, HmmWriter writer) {
            return HmmValue.Void;
        }
    }
}
