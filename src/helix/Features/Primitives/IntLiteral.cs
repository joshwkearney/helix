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
        private ISyntaxTree IntLiteral() {
            var tok = this.Advance(TokenKind.IntLiteral);
            var num = int.Parse(tok.Value);

            return new IntLiteral(tok.Location, num);
        }
    }
}

namespace Helix.Features.Primitives {
    public record IntLiteral : ISyntaxTree {
        public TokenLocation Location { get; }

        public int Value { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public IntLiteral(TokenLocation loc, int value) {
            this.Location = loc;
            this.Value = value;
        }

        public Option<HelixType> AsType(TypeFrame types) {
            return new SingularIntType(this.Value);
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            this.SetReturnType(new SingularIntType(this.Value), types);
            this.SetCapturedVariables(types);

            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            flow.SyntaxLifetimes[this] = new LifetimeBundle();
        }

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            return new CIntLiteral(this.Value);
        }
    }
}
