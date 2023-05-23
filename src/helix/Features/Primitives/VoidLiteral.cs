using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis;

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

        public Option<HelixType> AsType(TypeFrame types) => PrimitiveType.Void;

        public ISyntaxTree CheckTypes(TypeFrame types) {
            this.SetReturnType(PrimitiveType.Void, types);
            this.SetCapturedVariables(types);

            return this;
        }

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            flow.SyntaxLifetimes[this] = new LifetimeBundle();
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            return new CIntLiteral(0);
        }
    }
}
