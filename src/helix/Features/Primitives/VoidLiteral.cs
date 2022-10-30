using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Lifetimes;

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

        public Option<HelixType> AsType(EvalFrame types) => PrimitiveType.Void;

        public ISyntaxTree CheckTypes(EvalFrame types) {
            types.ReturnTypes[this] = PrimitiveType.Void;
            types.Lifetimes[this] = new LifetimeBundle();

            return this;
        }

        public ISyntaxTree ToRValue(EvalFrame types) => this;

        public ICSyntax GenerateCode(EvalFrame types, ICStatementWriter writer) {
            return new CIntLiteral(0);
        }
    }
}
