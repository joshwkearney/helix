using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

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

namespace Helix.Features.FlowControl {
    public record ReturnSyntax : ISyntaxTree {
        private readonly ISyntaxTree payload;
        private readonly IdentifierPath func;
        private readonly bool isTypeChecked = false;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.payload };

        public bool IsPure => false;

        public ReturnSyntax(TokenLocation loc, ISyntaxTree payload, 
            IdentifierPath func, bool isTypeChecked = false) {

            this.Location = loc;
            this.payload = payload;
            this.func = func;
            this.isTypeChecked = isTypeChecked;
        }

        public ISyntaxTree ToRValue(TypeFrame frame) {
            if (!this.isTypeChecked) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            var sig = types.Functions[this.func];
            var payload = this.payload.CheckTypes(types).ToRValue(types);
            var result = new ReturnSyntax(this.Location, payload, this.func, true);

            types.ReturnTypes[result] = PrimitiveType.Void;

            return result;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            flow.SyntaxLifetimes[this] = new LifetimeBundle();

            // Add a dependency on the heap for every lifetime in the result
            if (!this.GetReturnType(flow).IsValueType(flow)) {
                foreach (var time in flow.SyntaxLifetimes[this.payload].Values) {
                    flow.LifetimeGraph.RequireOutlives(time, Lifetime.Heap);
                }
            }
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
