using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Features.Functions;

namespace Helix.Parsing {
    public partial class Parser {
        public ISyntaxTree ReturnStatement() {
            var start = this.Advance(TokenKind.ReturnKeyword);
            var arg = this.TopExpression();

            return new ReturnSyntax(
                start.Location,
                arg, 
                this.funcPath.Peek());
        }
    }
}

namespace Helix.Features.Functions {
    public record ReturnSyntax : ISyntaxTree {
        private readonly ISyntaxTree payload;
        private readonly IdentifierPath funcPath;
        private readonly bool isTypeChecked = false;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.payload };

        public bool IsPure => false;

        public ReturnSyntax(TokenLocation loc, ISyntaxTree payload, 
            IdentifierPath func, bool isTypeChecked = false) {

            this.Location = loc;
            this.payload = payload;
            this.funcPath = func;
            this.isTypeChecked = isTypeChecked;
        }

        public ISyntaxTree ToRValue(TypeFrame frame) {
            if (!this.isTypeChecked) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            var payload = this.payload.CheckTypes(types).ToRValue(types);
            var result = new ReturnSyntax(this.Location, payload, this.funcPath, true);

            result.SetReturnType(PrimitiveType.Void, types);
            result.SetCapturedVariables(types);
            result.SetPredicate(types);

            return result;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            var sig = flow.Functions[this.funcPath];

            flow.SyntaxLifetimes[this] = new LifetimeBundle();
            FunctionsHelper.AnalyzeReturnValueFlow(this.Location, sig, this.payload, flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            writer.WriteStatement(new CReturn() {
                Target = this.payload.GenerateCode(types, writer)
            });

            return new CIntLiteral(0);
        }
    }
}
