using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Lifetimes;
using helix.Syntax;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree BoolLiteral() {
            var start = this.Advance(TokenKind.BoolLiteral);
            var value = bool.Parse(start.Value);

            return new BoolLiteral(start.Location, value);
        }
    }
}

namespace Helix.Features.Primitives {
    public record BoolLiteral : ISyntaxTree {
        public TokenLocation Location { get; }

        public bool Value { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public BoolLiteral(TokenLocation loc, bool value) {
            this.Location = loc;
            this.Value = value;
        }

        public Option<HelixType> AsType(EvalFrame types) {
            return new SingularBoolType(this.Value);
        }

        public ISyntaxTree CheckTypes(EvalFrame types) {
            types.ReturnTypes[this] = new SingularBoolType(this.Value);
            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            flow.Lifetimes[this] = new LifetimeBundle();
        }

        public ISyntaxTree ToRValue(EvalFrame types) => this;

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            return new CIntLiteral(this.Value ? 1 : 0);
        }
    }
}
